using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        var runTest = new Action<string, Action<TestContext>>((name, test) =>
        {
            var context = new TestContext();
            var oldColor = Console.ForegroundColor;
            Console.Write("{0} -> ", name);
            try
            {
                sw.Start();
                test(context);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("PASSED ");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("FAILED ");
                Console.Write(e.Message);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine(" [{0} ms]", sw.ElapsedMilliseconds);
                Console.ForegroundColor = oldColor;

                if (context.HasOutput)
                {
                    Console.WriteLine();
                    Console.WriteLine(context.GetContextOutput());
                }
                sw.Reset();
            }
        });

        // tests to run
        //runTest("VerifyEncodeDecodeList", ListEncodesAndDecodes);
        //runTest("CompressionYieldsLessStorage", CompressionYieldsLessStorage);
        //runTest("CanLocateKnownData", CanLocateKnownData);
        //runTest("CanStoreMultiValueData", CanStoreMultiValueData);
        runTest("LoadTestScenario", LoadTestScenario);
    }

    /// <summary>
    /// Make sure we can get a list back in the same forms.
    /// </summary>
    static void ListEncodesAndDecodes(TestContext context)
    {
        var rawList = new List<uint>();

        for (var j = 0; j < 10; j++)
        {
            rawList.Add((uint)j);
        }

        var encoder = new Base128Encoder();
        var decodedList = encoder.DecodeList(encoder.EncodeList(rawList));

        rawList.Sort();
        decodedList.Sort();

        Assert.AreEqual(rawList.Count, decodedList.Count);

        for (var m = 0; m < rawList.Count; m++)
        {
            Assert.AreEqual(rawList[m], decodedList[m]);
        }
    }

    /// <summary>
    /// Just a check to see how much space we're saving
    /// </summary>
    static void CompressionYieldsLessStorage(TestContext context)
    {
        // create a list of 500,000 uints
        // at 4bytes per uint, this should be ~1.9 - 2.0 MB
        var TEST_SIZE = 2000000;
        var docIds = new List<UInt32>(TEST_SIZE);
        var rng = new Random();

        for (var j = 0; j < TEST_SIZE; j++)
        {
            docIds.Add((uint)rng.Next(1, Int32.MaxValue));
        }

        // now compress that list
        var encoder = new Base128Encoder();
        var packedData = encoder.EncodeList(docIds);

        // How much space did we save
        context.WriteLine(String.Format("Raw size: {0} bytes", TEST_SIZE * 4));
        context.WriteLine(String.Format("Packed size: {0} bytes", packedData.Length));
    }

    /// <summary>
    /// Make sure we find exactly the known data we're after
    /// </summary>
    static void CanLocateKnownData(TestContext context)
    {
        var index = new Dictionary<string, byte[]>();
        var encoder = new Base128Encoder();

        // The term "cat" matches documents 2,4,6,8
        var docsWithCat = new List<uint>() { 2, 4, 6, 8 };
        index["cat"] = encoder.EncodeList(docsWithCat);

        // The term "dog" matches documents 6,8,10,12
        var docsWithDog = new List<uint>() { 6, 8, 10, 12 };
        index["dog"] = encoder.EncodeList(docsWithDog);

        // query for docs that have both "cat" and "dog"
        var results = new HashSet<uint>();
        var firstPass = true;

        foreach (var pair in index)
        {
            var idList = encoder.DecodeList(pair.Value);
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

        // Should have only found 6 and 8
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual((uint)6, results.ElementAt(0));
        Assert.AreEqual((uint)8, results.ElementAt(1));
    }

    /// <summary>
    /// Make sure we can save multidimensional data, using indirection
    /// on both the doc-ids _and_ terms.
    /// 
    /// Lets save a table like so:
    /// 
    ///     "author"    "years_experience"
    /// d0   "Bob"          5
    /// d0   "Bill"         3
    /// d0   "Nancy"        9
    /// 
    /// d1   "Tom"          2
    /// d1   "Derrick"      4
    /// d1   "Billy"        6
    /// 
    /// We should then be able to query for a column and get only those 
    /// values back that belong to a specific document. So our query case 
    /// could then be something like the following:
    /// 
    ///     "For all documents that have 'Bill' as an author, compute total combined
    ///     years of experience".
    ///     
    /// Based on the above dataset, this should yield:
    /// 
    ///     d0 : 17
    ///     
    /// </summary>
    static void CanStoreMultiValueData(TestContext context)
    {
        // index stuffs
        var termHashMap = new Dictionary<uint, TermValuePair>();
        var docHashMap = new Dictionary<uint, DocPositionPair>();        
        var index = new Dictionary<uint, byte[]>();
        var encoder = new Base128Encoder();

        // Add data for doc 0 (d0)
        addIndexValue("author", "Bob", 0, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("author", "Bill", 0, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("author", "Nancy", 0, 2, termHashMap, docHashMap, index, encoder);
        addIndexValue("years_experience", "5", 0, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("years_experience", "3", 0, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("years_experience", "9", 0, 2, termHashMap, docHashMap, index, encoder);

        // Add data for doc 1 (d1)
        addIndexValue("author", "Tom", 1, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("author", "Derrick", 1, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("author", "Billy", 1, 2, termHashMap, docHashMap, index, encoder);
        addIndexValue("years_experience", "2", 1, 0, termHashMap, docHashMap, index, encoder);
        addIndexValue("years_experience", "4", 1, 1, termHashMap, docHashMap, index, encoder);
        addIndexValue("years_experience", "6", 1, 2, termHashMap, docHashMap, index, encoder);

        // Our query case:
        //     "For all documents that have 'Bill' as an author, compute total combined
        //     years of experience".
        //

        // NOTE: These lookups are scans only to find the values of interest. If this
        // were coming from an actual UI, the user would have done this, and therefore 
        // these lookups would be skipped. This works because we associate a hash value 
        // with a term name _and_ value...each one is unique (collisions aside)
        
        // isloate terms of type "author" with value "Bill"
        var authorHashes = new HashSet<uint>(termHashMap
            .Where(k => k.Value.TermName == "author" && k.Value.Value == "Bill")
            .Select(u => u.Key));

        // isolate "years_experience" values
        var yearsExperienceHashes = new HashSet<uint>(termHashMap
            .Where(k => k.Value.TermName == "years_experience")
            .Select(u => u.Key));

        var docsThatHaveAuthor = new HashSet<uint>();
        var result = 0;

        // Find the docs ID's that have the specified author
        foreach (var hash in authorHashes)
        {
            foreach (var docHash in encoder.DecodeList(index[hash]))
            {
                docsThatHaveAuthor.Add(docHashMap[docHash].Identifier);
            }
        }

        // Find all years_experience values belonging to the found doc ID's
        foreach (var hash in yearsExperienceHashes)
        {
            foreach (var docHash in encoder.DecodeList(index[hash]))
            {
                // Hashset.contains is ~ O(1)
                if (docsThatHaveAuthor.Contains(docHashMap[docHash].Identifier))
                {
                    result += Convert.ToInt32(termHashMap[hash].Value);
                }
            }
        }

        // Check results
        Assert.AreEqual(17, result);
    }

    /// <summary>
    /// Helper method to add index entries
    /// </summary>
    static void addIndexValue(string termName, 
        string termValue, 
        uint docId, 
        uint position, 
        Dictionary<uint, TermValuePair> termHashMap, 
        Dictionary<uint, DocPositionPair> docHashMap, 
        Dictionary<uint, byte[]> index, 
        Base128Encoder encoder)
    {
        var termPair = new TermValuePair(termName, termValue);
        var termPairHash = termPair.ComputeHashValue();

        if (!termHashMap.ContainsKey(termPairHash))
        {
            termHashMap[termPairHash] = termPair;
        }

        var docPair = new DocPositionPair(docId, position);
        var docPairHash = docPair.ComputeHashValue();

        if (!docHashMap.ContainsKey(docPairHash))
        {
            docHashMap[docPairHash] = docPair;
        }

        if (!index.ContainsKey(termPairHash))
        {
            index[termPairHash] = encoder.EncodeList(new List<uint>() { docPairHash });
        }
        else
        {
            var currentList = encoder.DecodeList(index[termPairHash]);
            currentList.Add(docPairHash);
            index[termPairHash] = encoder.EncodeList(currentList);
        }
    }

    /// <summary>
    /// Original load-test scenario.
    /// </summary>
    static void LoadTestScenario(TestContext context)
    {
        // Test config
        var TERMS = 50000;
        var DOCS_PER_TERM = 10000;
        var TOTAL_DOCS = 100000;

        // test support
        var sw = new Stopwatch();
        var rng = new Random();

        // Our simple inverted index and an encoder
        // Note the index does not map term to list-of-docid...
        // Instead, it maps term to compressed range.
        var index = new Dictionary<string, byte[]>();
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
            // We'll just use 'j' in string form as our term
            index[j.ToString()] = encoder.EncodeList(docLinks);
        }

        ////////////////////////////////////////////////////////
        //
        // Search index data
        //

        Console.WriteLine("Ready:");
        Console.ReadLine();

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
                // decode
                var decodedList = encoder.DecodeList(termPair.Value);

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

        ////////////////////////////////////////////////////////
        //
        // Results summary
        //

        sw.Stop();
        context.WriteLine(string.Format("Found {0} results", outSet.Count));
        context.WriteLine(string.Format("Query time: {0}ms", sw.ElapsedMilliseconds));
    }
}