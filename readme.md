# Overview

Test case project to explore and demonstrate using base-128 varint encoding on a list of document id's, such as those
typically found in inverted index structures. The basic test case attempts to load an index with compressed data, and
then perform a query on that data for a simple (but exhaustive) criteria.

There are several test cases run from the main entry point (`TextCases`); see the code comments for test goals.

### Multi-value example

There is a simple test case written that demonstrates the notion of using an inverted index to save and query multi-value
conditions. This is accomplished by encapsulating the doc+position, as well as distinguishing between a term name (e.g.,
"author") and a term value (e.g., "Bill"). These individual pairs (doc + pos, and term + value) are then passed through
an FNV-32A hash to yield a familiar list of integers which can then be fully represented via an inverted index.

### Upcoming work

This work is currently supporting development of an isolated datastore leveraging these techniques to allow for nested,
multi-value "tables" of data within individual documents...more documentation to come.
