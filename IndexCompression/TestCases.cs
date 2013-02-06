using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IndexCompression;

static class TestCases
{
    /// <summary>
    /// Simple test case for encoding a random document space (worst-case?)
    /// on simple term inverted-index.
    /// 
    /// Note this could of course be multithreaded, etc. The test here is 
    /// about the encoding portion.
    /// </summary>
    static void Main()
    {
        // simple test runner
        var sw = new Stopwatch();
        var runTest = new Action<string, Action>((name, test) =>
        {
            Console.Write("{0} -> ", name);
            try
            {
                sw.Start();
                test();
                Console.Write("PASSED ");
            }
            catch (Exception e)
            {
                Console.Write("FAILED ");
                Console.WriteLine(e);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine("[{0} ms]", sw.ElapsedMilliseconds);
                sw.Reset();
            }
        });

        // tests to run
        runTest("VerifyEncodeDecodeList", ListEncodesAndDecodes);
        runTest("CanLocateKnownData", CanLocateKnownData);
        runTest("CanStoreMultiValueData", CanStoreMultiValueData);
        runTest("LoadTestScenario", LoadTestScenario);
    }

    /// <summary>
    /// Make sure we can get a list back in the same forms.
    /// </summary>
    static void ListEncodesAndDecodes()
    {
        var list1 = new List<uint>();

        for (var j = 0; j < 10; j++)
        {
            list1.Add((uint)j);
        }

        var encoder = new Base128Encoder();
        using (var encStream = new MemoryStream())
        {
            encoder.EncodeList(encStream, list1);
            encStream.Position = 0;

            var list2 = encoder.DecodeList(encStream);

            list1.Sort();
            list2.Sort();

            Assert.AreEqual(list1.Count, list2.Count);

            for (var m = 0; m < list1.Count; m++)
            {
                Assert.AreEqual(list1[m], list2[m]);
            }
        }
    }

    /// <summary>
    /// Make sure we find exactly the known data we're after
    /// </summary>
    static void CanLocateKnownData()
    {
        var index = new Dictionary<string, byte[]>();
        var encoder = new Base128Encoder();

        // The term "cat" matches documents 2,4,6,8
        var docsWithCat = new List<uint>() { 2, 4, 6, 8 };
        using (var encoderStream = new MemoryStream())
        {
            encoder.EncodeList(encoderStream, docsWithCat);
            index["cat"] = encoderStream.ToArray();
        }

        // The term "dog" matches documents 6,8,10,12
        var docsWithDog = new List<uint>() { 6, 8, 10, 12 };
        using (var encoderStream = new MemoryStream())
        {
            encoder.EncodeList(encoderStream, docsWithDog);
            index["dog"] = encoderStream.ToArray();
        }

        // query for docs that have both
        var results = new HashSet<uint>();
        var firstPass = true;
        foreach (var pair in index)
        {
            using (var decodeStream = new MemoryStream(pair.Value))
            {
                var idList = encoder.DecodeList(decodeStream);
                if (firstPass)
                {
                    firstPass = false;
                    results.UnionWith(idList);
                }
                else
                {
                    results.IntersectWith(idList);
                }
            }
        }

        // Should have only found 6 and 8
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual((uint)6, results.ElementAt(0));
        Assert.AreEqual((uint)8, results.ElementAt(1));
    }

    /// <summary>
    /// Make sure we can save multidimensional data, using indirection
    /// on the doc-ids.
    /// 
    /// Lets save a table like so:
    /// 
    ///     "col0"  "col1"  "col2"  "col3" "col4"   "col5"
    /// d0   NULL      b       c       d      e        f
    /// d0    gg      NULL     i       j      k        zz
    /// d0    m        n      NULL     p      q        r
    /// d1    s        t       u      NULL    w        x
    /// d1    y        z       0       1     NULL      zz
    /// d1    4        5       6       7      8       NULL
    /// 
    /// We should then be able to query for a column and get only those 
    /// values back that belong to a specific document. So our query case 
    /// could then be something like the following:
    /// 
    ///     "All documents that have 'gg' for 'col0' and 'zz' for 'col5'."
    ///     
    /// Based on the above dataset, this should yield only 'd0'.
    /// </summary>
    static void CanStoreMultiValueData()
    {
        // index stuffs
        var termHashMap = new Dictionary<uint, TermValuePair>();
        var docHashMap = new Dictionary<uint, DocPositionPair>();        
        var index = new Dictionary<uint, byte[]>();
        var encoder = new Base128Encoder();

        // Add table data, one column at a time encoded off of the above matrix
        addIndexValue("col0", "g", 0, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("col0", "m", 0, 2, termHashMap, docHashMap, index, encoder);
        addIndexValue("col0", "s", 1, 3, termHashMap, docHashMap, index, encoder);
        addIndexValue("col0", "y", 1, 4, termHashMap, docHashMap, index, encoder);
        addIndexValue("col0", "4", 1, 5, termHashMap, docHashMap, index, encoder);

        addIndexValue("col1", "b", 1, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("col1", "n", 1, 2, termHashMap, docHashMap, index, encoder);
        addIndexValue("col1", "t", 1, 3, termHashMap, docHashMap, index, encoder);
        addIndexValue("col1", "z", 1, 4, termHashMap, docHashMap, index, encoder);
        addIndexValue("col1", "5", 1, 5, termHashMap, docHashMap, index, encoder);

        addIndexValue("col2", "c", 1, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("col2", "i", 1, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("col2", "u", 1, 3, termHashMap, docHashMap, index, encoder);
        addIndexValue("col2", "0", 1, 4, termHashMap, docHashMap, index, encoder);
        addIndexValue("col2", "6", 1, 5, termHashMap, docHashMap, index, encoder);

        addIndexValue("col3", "d", 1, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("col3", "j", 1, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("col3", "p", 1, 2, termHashMap, docHashMap, index, encoder);
        addIndexValue("col3", "1", 1, 4, termHashMap, docHashMap, index, encoder);
        addIndexValue("col3", "7", 1, 5, termHashMap, docHashMap, index, encoder);

        addIndexValue("col4", "e", 1, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("col4", "k", 1, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("col4", "q", 1, 2, termHashMap, docHashMap, index, encoder);
        addIndexValue("col4", "w", 1, 3, termHashMap, docHashMap, index, encoder);
        addIndexValue("col4", "8", 1, 5, termHashMap, docHashMap, index, encoder);

        addIndexValue("col5", "f", 1, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("col5", "l", 1, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("col5", "r", 1, 2, termHashMap, docHashMap, index, encoder);
        addIndexValue("col5", "x", 1, 3, termHashMap, docHashMap, index, encoder);
        addIndexValue("col5", "3", 1, 4, termHashMap, docHashMap, index, encoder);

        // query for a specifc column, say "col3"
        var results = new List<DocPositionPair>();
        var col3Hashes = new HashSet<uint>(termHashMap
            .Where(k => k.Value.Archetype == "col3")
            .Select(u => u.Key));

        // walk index entires
        foreach (var pair in index)
        {
            // see if this entry is from our target population
            if (col3Hashes.Contains(pair.Key))
            {
                //// decode the hashes back to the original doc+position data
                //using (var decodeStream = new MemoryStream(pair.Value))
                //{
                //    var decodedList = encoder.DecodeList(decodeStream);

                //    foreach (var docHash in decodedList)
                //    {
                //        results.Add(docHashMap[docHash]);
                //    }
                //}
            }
        }

        // Check results
    }

    /// <summary>
    /// Helper method to add index entries
    /// </summary>
    static void addIndexValue(string termArchetype, 
        string termValue, 
        int docId, 
        int position, 
        Dictionary<uint, TermValuePair> termHashMap, 
        Dictionary<uint, DocPositionPair> docHashMap, 
        Dictionary<uint, byte[]> index, Base128Encoder encoder)
    {
        //
    }

    /// <summary>
    /// Original load-test scenario.
    /// </summary>
    static void LoadTestScenario()
    {
        // Test config
        var TERMS = 10000;
        var DOCS_PER_TERM = 1000;
        var TOTAL_DOCS = 10000;

        // test support
        var sw = new Stopwatch();
        var rng = new Random();

        // Our simple inverted index and an encoder
        // Note the index does not map term to list-of-docid...
        // Instead, it maps term to compressed range.
        var index = new ConcurrentDictionary<string, byte[]>();
        var encoder = new Base128Encoder();

        ////////////////////////////////////////////////////////
        //
        // Create index data
        //
        for (var j = 0; j < TERMS; j++)
        {
            var docLinks = new List<uint>();

            // add document links
            for (var m = 0; m < DOCS_PER_TERM; m++)
            {
                docLinks.Add((uint)rng.Next(1, TOTAL_DOCS)); // assume our doc count is 10K
            }

            // compress the docID list
            using (var encodingStream = new MemoryStream())
            {
                encoder.EncodeList(encodingStream, docLinks);

                // We'll just use 'j' in string form as our term
                index[j.ToString()] = encodingStream.ToArray();
            }
        }

        ////////////////////////////////////////////////////////
        //
        // Search index data
        //

        // Let's search for 'terms' that begin with '5000'
        // We then want all docs that are in both terms
        var query = new Predicate<string>((s) => s.StartsWith("5000"));
        var outSet = new HashSet<uint>();
        var firstPass = true;
        sw.Start();

        // how about just a simple linear scan for now
        foreach (var termPair in index)
        {
            if (query(termPair.Key))
            {
                using (var decodeStream = new MemoryStream(termPair.Value))
                {
                    // decode
                    var decodedList = encoder.DecodeList(decodeStream);

                    // can't intersect with an empty set - always result in nullset.
                    if (firstPass)
                    {
                        firstPass = false;
                        outSet.UnionWith(decodedList);
                    }
                    else
                    {
                        outSet.IntersectWith(decodedList);
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////
        //
        // Results summary
        //

        sw.Stop();
        Console.WriteLine("\t\tQuery finished: {0}ms", sw.ElapsedMilliseconds);
    }
}