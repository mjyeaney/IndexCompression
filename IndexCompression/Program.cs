using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

static class Program
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
        // Test config
        var TERMS = 10000;
        var DOCS_PER_TERM = 5000;
        var TOTAL_DOCS = 10000;

        // test support
        var sw = new Stopwatch();
        var rng = new Random();

        // Our simple inverted index and an encoder
        // Note the index does not map term to list-of-docid...
        // Instead, it maps term to compressed range.
        var index = new ConcurrentDictionary<string, byte[]>();
        var encoder = new Base128Encoding();

        Console.WriteLine("Ready to start test cases - press enter to start...");
        Console.ReadLine();
        sw.Start();

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

        Console.WriteLine("Dataset created...[{0} X {1}] matrix", TERMS, DOCS_PER_TERM);
        Console.WriteLine("Encoded in {0}ms", sw.ElapsedMilliseconds);
        Console.ReadLine();
        sw.Reset();

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
        // Search index data
        //

        sw.Stop();
        Console.WriteLine("Query finished: {0}ms", sw.ElapsedMilliseconds);
        Console.WriteLine("Found {0} documents...", outSet.Count);
        Console.ReadLine();
    }
}