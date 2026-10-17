### Record types
Record types take a string[] and returns an object (either a string, or string[]) to be stored
```coffeescript
para {
    # sets boundaries on the whole document, not on
    #  each paragraph
    include: section{} #optional
    
    # indicates the start of a new paragraph
    start:exp{} #required.
    
    # indicates the end of a paragraph
    stop:exp{} #optional
    
    # the list of propery parsers.
    # you can include any Record type
    props: [ record{} record{} ... ] #required.
}

table {
    # sets boundaries on the whole document, not on
    #  each paragraph
    include: section{} #optional
    
    # identifies a header line.
    # this is used to figure out the location
    #  of columns.  This is required if the
    #  column parser is used with the 'n' property
    header:exp{} #optional
    
    # identifies which lines have rows in them.
    row:exp{}

    # the list of propery parsers.
    # you can include any Record type
    parsers: [ Extractor{} Extractor{} ... ] #required.

    # true/false
    validate: "true"
}

prop {
    name:"val" #required
    
    # if the parser is for a paragraph, then this is required.
    # if the parser is for a table, then this is not needed.
    line:exp{} #optional
    
    # if present, the string[] is combined and 
    #  split again using the provided delimiter
    split:"val" #optional
    
    # the list of parsers to apply to the string
    parsers: [ Extractor{} Extractor{} ... ] #required
}

```

### Blocks
```coffeescript

section {
 start: exp{} #optional
 stop: exp{} #optional
}

```

### Extractor types
```coffeescript
to {
    # array of strings to search for.
    #  parses to instance of first item 
    #  in array found.
    # i == ignore case
    # w == include search pattern length in index
    # t == trim the returned value
    # l == to last instance of search pattern
    s:["val"]iwtl #required
    
    # quantifier: after specific instance of s value
    q:"2" #optional, if populated, l is ignored
}

after {
    # array of strings to search for.
    #  parses after instance of first item 
    #  in array found.
    # i == ignore case
    # w == include search pattern length in index
    # t == trim the returned value
    # l == after last instance of search pattern
    s:["val"]iwtl #required

    # quantifier: after specific instance of s value
    q:"2" #optional, if populated, l is ignored
}

split {
    # split the string on the provided values
    # i == ignore case
    # r == remove empty entries
    # t == trim the returned value
    s:["val"]irt #required
    
    # index of item to return
    i:"2"  #required
}

# this only works with the table parser
#  either the i or the n option must be used.
col {
    i:"3" #optional
    n:"column_name" #optional
}
```


### Exp types
```coffeescript
regex | reg {
    # The search parameter
    # i == ignore case
    # n == negate
    s:["val"]ni #required
}

contains {
    # The search parameter
    # i == ignore case
    # n == negate
    s:["val"]ni #required
}

startswith | starts {
    # The search parameter
    # i == ignore case
    # n == negate
    s:["val"]ni #required
}

endswith | ends {
    # The search parameter
    # i == ignore case
    # n == negate
    s:["val"]ni #required
}

# classic boolean and
and {
    contains{}
    startswith{}
    # etc..  
}

# classic boolean or
or {
    contains{}
    startswith{}
    # etc..  
}

# is the string s behind string a and ahead of string b
#  each list uses its own i modifier for its own matching, including
#  finding the last a and the last b that bound the search window.
#  n on s negates the whole result, so a missing a or b also flips it.
#  n on a or b has no meaning and is ignored.
look {
    a:["val"]i # optional
    # i == ignore case
    # n == negate
    s:["val"]ni #required
    b:["val"]i # optional
}

# the number of times a string may be contained in test string.
count { 
    # i == ignore case
    # n == negate
    s:["val"]ni #required

    # required.  as long as the number of instances of the val
    #  fall into the provided ranges, it returns true.
    rng:"1-5,7,8-10" #required
}
```

### Example

#### Test Data
```
blah
blah
foo
start
 this is a line
 this is another line
start
 this is foo
 this is more foo
bar
start
 this is foo
 this is more foo
```

#### Spec
```coffeescript
para {
    #optional include
    include: section {
        start: startswith {s:["foo"]}
        stop: startswith {s:["bar"]}
    }
    start: contains {s:["start"] m:"i"}
    props:[
        prop {
            name:"jason"
            line: contains {s:["is"] m:"i"}
            parsers:[
                after {
                    s:["is"]
                }
                to {
                    s:["line"]
                }

            ]
        }
        prop {
            name:"wall"
            line: contains {s:["an"] m:"i"}
            split:"foo" #optional
            parsers:[
                to {
                    s:["is"]
                }
            ]
        }
    ]
}

```