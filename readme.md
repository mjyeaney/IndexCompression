# Overview

Test case project to explore and demonstrate using base-128 varint encoding on a list of document id's, such as those
typically found in inverted index structures. The basic test case attempts to load an index with compressed data, and
then perform a query on that data for a simple (but exhaustive) criteria.

Config parameters may be tweaked by changing any of the following parameters in `Program.cs`:

```
// Test config
var TERMS = 10000;
var DOCS_PER_TERM = 1000;
var TOTAL_DOCS = 10000;
```

This base configuration yields a dense matrix sized at [10,000 X 1,000], containing 10,000,000 document id's to search
through.

### Performance

On my current machine (Core i7 920 @ 2.67GHz, 8GB Ram, Win 7x64, .NET 4.5), the index load is running in around ~1.5
sec, with the query running in ~20 msec. Not too bad, but this isn't a very large index either.

The current setup is also interesting in that the load time varies the greatest in realtion to the index size; query
time is fairly stable. This means the logic is holding up, or doesn't work at all. Hrmph.

### Known Issues

* Not sure the encoder is correctly returning d-gap lists correctly yet...this code is still being flushed out and un-tested. Working on
cleaning that up.
* This project is only for exploring query performance on compressed data lists; persistence is not handled at all, nor is
this attempting to be a complete data store (working on another project for that).
* Would like to add and compare to P4Delta encoding
* Other tests for Huffman string encoding would also be valuable.
