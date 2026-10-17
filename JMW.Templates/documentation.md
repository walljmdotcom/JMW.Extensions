# Templating Tag Reference


## Tag: `<if>`

#### Allowed Props


| Attr   | Description |
| ----   | ----------- |
| `column` | **conditional**: Accepts the name of the column whose value you are evaluating.  This is required if you use the `<if>` tag inside a `<table>`, `<join>`, or `<lookup>` tag and want to use the value of one of the columns. |
| `exp`    | **conditional**: Accepts a javascript expression that must return a boolean value. The last expression evaluated is what is returned. either this or the `type` and `value` attributes are required. |
| `value`  | **conditional**: A value to compare to the value of the column |
| `type`   | **conditional**: The kind of comparison: equals, notequals, startswith, notstartswith, contains, notcontains, endswith, notendswith |
| `oct`    | **optional**: Accepts a number between 1-4, and specifies the octet of an ip to send to the "exp" attribute |

If the `exp` attribute is present, the `value` and `type` attributes, if present, will be ignored.

#### Examples

```xml
<if column="col1, col2" exp="x1 == 1 && x2 == 2">
   print this text;
</if>
```

```xml
<if column="col1" value="1" type="equals">
   print this text;
</if>
```
<br>
<br>

## Tag: `<include>`

#### Allowed Props


| Attr | Description |
| ---- | ----------- |
| `path` | **conditional**: Accepts a file path to a text file containing template text |
| `name` | **conditional**: Allows you to reference a defined include |
| `define` | **conditional**: Allows you to define a named include to use elsewhere in the template |

When an `<include>` is used with the `name` attribute, there must be corresponding
`<include define=string.Empty>` tag somewhere in the template.

Only one of the attributes can be used at a time.

A `path` is resolved against the interpreter's `FileRoot`, which defaults to the current directory, and must stay inside it. A path that leaves the root, whether absolute or through `..`, is rejected with an `UnauthorizedAccessException`. Set `RestrictFilePaths = false` on the `Interpreter` to allow any path, but only when you trust every template it will run.

Included files can include other files. Includes that form a loop, or nest more than 32 levels deep, are an error.

#### Examples

##### Template 1
```xml
<include define="foo">
   print this text;\n
</include>

this is a template.  I want to print the foo block here:
<include name="foo"/>
and i also want to print it here:
<include name="foo"/>
```
##### Output 1

```
this is a template.  I want to print the foo block here:
   print this text;
and i also want to print it here:
   print this text;

```
##### Files 2
###### foo.tmpl (in the FileRoot directory)
```
   foo text, lets here it!
```

###### parts\bar.txt
```
   bar text, we are the best!
```

##### Template 2
```xml
this is a template.  I want to print template text from foo.tmpl here:\n
<include path="foo.tmpl"/>\n
and i also want to print template text fro bar.txt here:\n
<include path="parts\bar.txt"/>\n
```

##### Output 2
```
this is a template.  I want to print template text from foo.tmpl here:
   foo text, lets here it!
and i also want to print template text fro bar.txt here:
   bar text, we are the best!
```
<br>
<br>

## Tag: `<table>`

`<table>` tags allow you to access data from loaded data sources.  Often this comes 
from a csv, excel file, or a dataset that was passed to the templating engine.

The interior of the tag is processed for each row in the table identified by the `name` 
attribute.

#### Allowed Props


| Attr | Description |
| ---- | ----------- |
| `name` | **required**: The name of the spreadsheet tab or table in the dataset to pull data from.  If this is a csv then the name can be empty.  If a csv is used, only 1 table source is allowed in the template, and if a `name` attribute is provided it will cause an exception. |
| `where` | **optional**: Allows you to filter the rows of the table.  The value is a javascript expression that returns a boolean (true/false).  The last expression evaluated is what is returned.  Columns are referenced ordinally (x1 is the first column, x2 is the second, etc...) |
| `distinct` | **optional**: Allows you to create a distinct list of rows.  The value is a comma delimited list of column names.  These columns will be the only columns available in the table. |
| `orderby` | **optional**: Sorts the data using the column specified.  Only one column is allowed. |
| `order` | **optional**: By default, `orderby` is sorted in ascending order.  If the `order` attribute is provided, and the value is set to `desc` it will sort in descending order. |

The `where` and `distinct` attributes can be used together.

### Sub Tag: `<column/>`

Column tags are named the same name as the column of a table.

##### Allowed Props

| Attr | Description |
| ---- | ----------- |
| exp  | **optional**: Accepts a javascript expression that must return a boolean value.  The last expression evaluated is what is returned. |
| oct  | **optional**: Accepts a number between 1-4, and specifies the octet of an ip to send to the "exp" attribute |

#### Examples

##### Input Table

| col1 | col2 | col3 | col4 |
| ---- | ---- | ---- | ---- |
| foo1 | foo2 | 1 | 1 |
| bar1 | bar2 | 3 | 3 |
| baz1 | baz2 | 5 | 1 |
| fuz1 | fuz2 | 7 | 2 |
| fee1 | fee2 | 9 | 1 |

##### Template 1 (simple table)
```xml
<table name="foo">
   Column 1: <col1/> | Column 2 <col2/>\n
</table>
```
##### Output 1

```
Column 1: foo1 | Column 2 foo2\n
Column 1: bar1 | Column 2 bar2\n
Column 1: baz1 | Column 2 baz2\n
Column 1: fuz1 | Column 2 fuz2\n
Column 1: fee1 | Column 2 fee2\n
```

##### Template 2 (where clause)
```xml
<table name="foo" where="x3 > 1 && x3 < 9">
   Column 1: <col1/> | Column 2 <col2/>\n
</table>
```
##### Output 2

```
Column 1: bar1 | Column 2 bar2\n
Column 1: baz1 | Column 2 baz2\n
Column 1: fuz1 | Column 2 fuz2\n
```
 

##### Template 3 (distinct)
```xml
<table name="foo" distinct="col4">
   Column 1: <col1/> | Column 2 <col2/> | Column 3: <col3/> | Column 4: <col4/>\n
</table>
```
##### Output 3

```
Column 1: foo1 | Column 2 foo2 | Column 3: 1 | Column 4: 1\n
Column 1: bar1 | Column 2 bar2 | Column 3: 3 | Column 4: 3\n
Column 1: fuz1 | Column 2 fuz2 | Column 3: 7 | Column 4: 2\n
```


##### Template 4 (orderby)
```xml
<table name="foo" distinct="col4" orderby="col4">
   Column 1: <col1/> | Column 2 <col2/> | Column 3: <col3/> | Column 4: <col4/>\n
</table>
```
##### Output 4

```
Column 1: foo1 | Column 2 foo2 | Column 3: 1 | Column 4: 1\n
Column 1: fuz1 | Column 2 fuz2 | Column 3: 7 | Column 4: 2\n
Column 1: bar1 | Column 2 bar2 | Column 3: 3 | Column 4: 3\n
```


##### Template 5 (orderby, order="desc")
```xml
<table name="foo" distinct="col4" orderby="col4" order="desc">
   Column 1: <col1/> | Column 2 <col2/> | Column 3: <col3/> | Column 4: <col4/>\n
</table>
```
##### Output 5

```
Column 1: bar1 | Column 2 bar2 | Column 3: 3 | Column 4: 3\n
Column 1: fuz1 | Column 2 fuz2 | Column 3: 7 | Column 4: 2\n
Column 1: foo1 | Column 2 foo2 | Column 3: 1 | Column 4: 1\n
```

<br>
<br>

## Tag: `<join>`

`<join>` tags allow you to join two tables loaded from data sources.  Often this comes 
from a csv, excel file, or a dataset that was passed to the templating engine.

The interior of the tag is processed for each row of the data joined from the two tables 
identified by the `left_table` and `right_table` attributes.

At this time, only an inner join is performed.  This means that only rows where the provided
key is present in both tables will be included.

When a join is performed, the columns of the tables are prefixed with `left_` and `right_`.  
This means that if the left table has a column called `col1` and then in the joined table it
will be called `left_col1` and if its from the right table then `right_col1`.  **This is true
even if the column names are unique.**

#### Allowed Props


| Attr | Description |
| ---- | ----------- |
| `left_table` | **required**: The name of the spreadsheet tab or table in the dataset to pull data from.  If this is a csv then the name can be empty.  If a csv is used, only 1 table source is allowed in the template, and if a `left_table` attribute is provided with a value it will cause an exception. |
| `left_key_exp` | **conditional**: Allows you to specify the join key using a javascript expression that returns a string.  The last expression evaluated is what will be returned. |
| `left_key_col` | **conditional**: Allows you to specify a column whose value will be used to join.  `left_key_col` and `left_key_exp` cannot be used together.  One or the other must be present.  If both are present, the `left_key_exp` will be used and the other ignored.|
| `left_where` | **optional**: Allows you to filter the rows of the table before it is joined.  The value is a javascript expression that returns a boolean (true/false).  The last expression evaluated is what is returned.  Columns are referenced ordinally (x1 is the first column, x2 is the second, etc...) |
| `right_table` | **required**: The name of the spreadsheet tab or table in the dataset to pull data from.  If this is a csv then the name can be empty.  If a csv is used, only 1 table source is allowed in the template, and if a `right_table` attribute is provided with a value it will cause an exception. |
| `right_key_exp` | **conditional**: Allows you to specify the join key using a javascript expression that returns a string.  The last expression evaluated is what will be returned. |
| `right_key_col` | **conditional**: Allows you to specify a column whose value will be used to join.  `right_key_col` and `right_key_exp` cannot be used together.  One or the other must be present.  If both are present, the `right_key_exp` will be used and the other ignored. |
| `right_where` | **optional**: Allows you to filter the rows of the table before it is joined.  The value is a javascript expression that returns a boolean (true/false).  The last expression evaluated is what is returned.  Columns are referenced ordinally (x1 is the first column, x2 is the second, etc...) |
| `where` | **optional**: Allows you to filter the rows of the joined table.  The value is a javascript expression that returns a boolean (true/false).  The last expression evaluated is what is returned.  Columns are referenced ordinally (x1 is the first column, x2 is the second, etc...) |
| `distinct` | **optional**: Allows you to create a distinct list of rows.  The value is a comma delimited list of column names.  These columns will be the only columns available in the table. |
| `orderby` | **optional**: Sorts the data using the column specified.  Only one column is allowed. |
| `order` | **optional**: By default, `orderby` is sorted in ascending order.  If the `order` attribute is provided, and the value is set to `desc` it will sort in descending order. |

The `where`, `distinct`, and `orderby` attributes can be used together.

### Sub Tag: `<column/>`

Column tags are named the same name as the column of a table, with the addition of `left_` or `right_` prefixes added to
the left and right table column names.

##### Allowed Props

| Attr | Description |
| ---- | ----------- |
| `exp`  | **optional**: Accepts a javascript expression that must return a boolean value.  The last expression evaluated is what is returned. |
| `oct`  | **optional**: Accepts a number between 1-4, and specifies the octet of an ip to send to the "exp" attribute |

#### Examples

##### Input Table (name: table1)

| col1 | col2 | col3 | col4 |
| ---- | ---- | ---- | ---- |
| foo1 | foo2 | 1 | 1 |
| bar1 | bar2 | 3 | 3 |
| baz1 | baz2 | 5 | 1 |
| fuz1 | fuz2 | 7 | 2 |
| fee1 | fee2 | 9 | 1 |

##### Input Table (name: table2)

| col1 | col2 | col3 | col4 |
| ---- | ---- | ---- | ---- |
| f1 | f2 | 1 | 10 |
| b1 | b2 | 3 | 30 |
| z1 | z2 | 5 | 10 |
| u1 | u2 | 7 | 20 |
| e1 | e2 | 9 | 10 |

##### Template 1 (simple join)
```xml
<join left_table="table1" left_key_exp="x3" right_table="table2" right_key_col="col3">
   table1:col1: <left_col1/> | table2:col2 <right_col2/>\n
</join>
```
##### Output 1

```
table1:col1: foo1 | table2:col2 f2
table1:col1: bar1 | table2:col2 b2
table1:col1: baz1 | table2:col2 z2
table1:col1: fuz1 | table2:col2 u2
table1:col1: fee1 | table2:col2 e2
```


##### Template 2 (left_where, right_where, orderby, order)
```xml
<join  left_table="table1"  left_key_exp="x3"    left_where="x2 != 'baz2'" 
      right_table="table2" right_key_col="col3" right_where="x3 > 1 && x3 < 9"
      orderby"right_col3" order="desc"
      >
   table1:col1: <left_col1/> | table2:col2 <right_col2/>\n
</join>
```
##### Output 2

```
table1:col1: fuz1 | table2:col2 u2
table1:col1: bar1 | table2:col2 b2
```

##### Template 1 (complex_key, where)
```xml
<join  left_table="table1"  left_key_exp="x3+x4+'0'" 
      right_table="table2" right_key_exp="x3+x4"
      where="x8 > 10"
      >
   table1:col1: <left_col1/> | table2:col2 <right_col2/>\n
</join>
```
##### Output 1

```
table1:col1: bar1 | table2:col2 b2
table1:col1: fuz1 | table2:col2 u2
```


<br>
<br>

## Tag: `<lookup>`

`<lookup>` tags allow you to index a table and lookup rows that match the key.

The interior of the tag is processed for each row of the data joined from the table 
identified by the `table` attribute.

#### Allowed Props

| Attr | Description |
| ---- | ----------- |
| `table` | **required**: The name of the table you are going to index |
| `key_exp` | **conditional**: Allows you to specify the key using a javascript expression that returns a string.  The last expression evaluated is what will be returned. |
| `key_col` | **conditional**: Allows you to specify a column by name whose value will be used as the key.  `key_col` and `key_exp` cannot be used together.  One or the other must be present.  If both are present, the `key_exp` will be used and the other ignored.|

The `where`, `distinct`, and `orderby` attributes can be used together.

### Sub Tag: `<row/>`

Row tags work similar to `<table>` tags, in that they use `<column>` tags the same way.  You provide a key value in the `key`
attribute and build a template inside the `<row>` and `</row>` tags.

##### Allowed Props

| Attr | Description |
| ---- | ----------- |
| `key`  | **required**: The key value used to retrieve 1 or more rows from the lookup index. |


### Sub Tag: `<column/>`

Column tags are named the same name as the columns of the table referenced by the `<lookup>` tag.

##### Allowed Props

| Attr | Description |
| ---- | ----------- |
| `exp`  |**optional**: Accepts a javascript expression that must return a boolean value.  The last expression evaluated is what is returned. |
| `oct`  |**optional**: Accepts a number between 1-4, and specifies the octet of an ip to send to the "exp" attribute |


#### Examples

##### Input Table (name: table1)

| col1 | col2 | col3 | col4 |
| ---- | ---- | ---- | ---- |
| foo1 | foo2 | 1 | 1 |
| bar1 | bar2 | 3 | 2 |
| baz1 | baz2 | 5 | 1 |
| fuz1 | fuz2 | 7 | 3 |
| fee1 | fee2 | 9 | 1 |

##### Template 1 (simple lookup)
```xml
<lookup table="table1" key_exp="x4">
  These rows are ones where col4 is equal to "1"\n
  <row key="1">
   <col1/> | <col2/> | <col3/> | <col4/>\n
  </row>
  These rows are ones where col4 is equal to "2"\n
  <row key="2">
   <col1/> | <col2/> | <col3/> | <col4/>\n
  </row>
</lookup>
```
##### Output 1

```
  These rows are ones where col4 is equal to "1"
   foo1 |  foo2 | 1 |  1
   baz1 |  baz2 | 5 |  1
   fee1 |  fee2 | 9 |  1
  These rows are ones where col4 is equal to "2"
   bar1 |  bar2 | 3 |  2
```

<br>
<br>

## Tag: `<output>`

`<output>` tags allow you to divert the processed output to a file.  The contents of the output tag will
be written to a location specified by the `filename` attribute.

#### Allowed Props


| Attr | Description |
| ---- | ----------- |
| `filename` | **required**: The name of the file you want to write to.  Relative paths are resolved against the interpreter's `FileRoot`, and the file must be inside it unless `RestrictFilePaths` is false (see `<include>`). |
| `mode` |**optional**: Can have a value of `new` or `append`.  `append` is the default value. If a value of `new` is present, then a file will be created each time the tag is encountered, and the `filename` attribute will append an iteration count to the end fo the filename.  If `append` is used, the data will be added to the end of the file.  If the `excel` attribute is present the data will be appended to the specified sheet.  If `append` is used a new column will be created for each encounter of the `<output>` tag.|
| `excel` |**optional**: If present, this indicates that the output format should be an excel file.  The value of this attribute is the name of the sheet to write the data to. |
| `column_name` |**optional**: The name of the column used when writing to an excel sheet.  Default value is `output`.|

#### Examples

##### Input Table (name: table1)

| col1 | col2 | col3 | col4 |
| ---- | ---- | ---- | ---- |
| foo1 | foo2 | 1 | 1 |
| bar1 | bar2 | 3 | 2 |
| baz1 | baz2 | 5 | 1 |
| fuz1 | fuz2 | 7 | 3 |
| fee1 | fee2 | 9 | 1 |

##### Template 1
```xml
<table name="table1">
<output filename="foo.txt">
   <col1/> | <col2/> | <col3/> | <col4/>\n
</output>
</table>
```
##### Output 1 (foo.txt)
```
   foo1 |  foo2 | 1 |  1
   bar1 |  bar2 | 3 |  2
   baz1 |  baz2 | 5 |  1
   fuz1 |  fuz2 | 7 |  3
   fee1 |  fee2 | 9 |  1
```

##### Template 2
```xml
<table name="table1">
<output filename="foo.txt" mode="new">
   <col1/> | <col2/> | <col3/> | <col4/>\n
</output>
</table>
```
##### Output foo1.txt
```
   foo1 |  foo2 | 1 |  1
```
##### Output foo2.txt
```
   bar1 |  bar2 | 3 |  2
```
##### Output foo3.txt
```
   baz1 |  baz2 | 5 |  1
```
##### Output foo4.txt
```
   fuz1 |  fuz2 | 7 |  3
```
##### Output foo5.txt
```
   fee1 |  fee2 | 9 |  1
```


##### Template 3
```xml
<table name="table1">
<output filename="foo.xlsx" mode="new" excel="sheet1" column_name="foo">
   <col1/>, <col2/>, <col3/>, <col4/>\n
</output>
</table>

```
##### Output foo.xlsx
| foo1 | foo2 | foo3 | foo4 | foo5 |
| ---- | ---- | ---- | ---- | ---- |
| foo1, foo2, 1, 1 | bar1, bar2, 3, 2 | baz1, baz2, 5, 1 | fuz1, fuz2, 7, 3 | fee1, fee2, 9, 1 |

##### Template 4
```xml
<table name="table1">
<output filename="foo.xlsx" mode="append" excel="sheet1">
   <col1/>, <col2/>, <col3/>, <col4/>\n
</output>
</table>

```
##### Output foo.xlsx
| output |
| ---- | 
| foo1, foo2, 1, 1 |
| bar1, bar2, 3, 2 |
| baz1, baz2, 5, 1 |
| fuz1, fuz2, 7, 3 |
| fee1, fee2, 9, 1 |

<br>
<br>

## Tag: `<var>`

`<var>` tags allow you to define and use variables in the template.

#### Allowed Props

| Attr     | Description |
| -------- | ----------- |
| `exp`   |**optional**: Accepts a javascript expression that must return a boolean value.  The last expression evaluated is what is returned. |
| `oct`   |**optional**: Accepts a number between 1-4, and specifies the octet of an ip to send to the "exp" attribute |
| `name`  |**conditional**: Allows you to create a variable with the provided name.  This is required if the `value` attribute is present.|
| `value` |**conditional**: Sets the value of the variable.  This is required if the `name` attribute is present |
| `store` |**optional**: `true`/`false`.  Defaults to `false`.  If set to `true` then the value of the variable is set with the value of the `exp` attribute once it has been evaluated. |
| `print` |**optional**: `true`/`false`.  Defaults to `true`.  If set to `false` the value is not printed.  This is usually used in conjuction with the `store` attribute, to update the value of the variable, but not print it. |

Variables are referenced by using this format: `<var:varname/>`.  

Variables can be used in any javascript expression by using this syntax: `exp="[var:varname] == 'foo'"`.


#### Examples


##### Template 1
```xml
<var name="foo" value="0" />

Lorem ipsum dolar sid amet: <var:foo/>
Lorem ipsum dolar sid amet: <var:foo exp="x1++"/>
Lorem ipsum dolar sid amet: <var:foo/>
Lorem ipsum dolar sid amet: <var:foo exp="x1++"/>
Lorem ipsum dolar sid amet: <var:foo>
```
##### Output 1
```
Lorem ipsum dolar sid amet: 0
Lorem ipsum dolar sid amet: 1
Lorem ipsum dolar sid amet: 0
Lorem ipsum dolar sid amet: 1
Lorem ipsum dolar sid amet: 0
```

##### Template 2
```xml
<var name="foo" value="0" />

Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
```
##### Output 2
```
Lorem ipsum dolar sid amet: 1
Lorem ipsum dolar sid amet: 2
Lorem ipsum dolar sid amet: 3
Lorem ipsum dolar sid amet: 4
Lorem ipsum dolar sid amet: 5
```

##### Template 3
```xml
<var name="foo" value="0" />

Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
Lorem ipsum dolar sid amet: <var:foo exp="++x1"/>
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
Lorem ipsum dolar sid amet: <var:foo exp="++x1"/>
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
```
##### Output 3
```
Lorem ipsum dolar sid amet: 1
Lorem ipsum dolar sid amet: 2
Lorem ipsum dolar sid amet: 2
Lorem ipsum dolar sid amet: 3
Lorem ipsum dolar sid amet: 3
```

##### Template 4
```xml
<var name="foo" value="0" />

<if exp="[var:foo]==2">
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
</if>
<if exp="[var:foo]==2">
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
</if>
<if exp="[var:foo]==3">
Lorem ipsum dolar sid amet: <var:foo exp="++x1" store="true"/>
</if>
```

##### Output 4
```
Lorem ipsum dolar sid amet: 2
Lorem ipsum dolar sid amet: 3
```
<br>
<br>

## Tag: `<timestamp>`

`<timestamp>` tag allows you to print the current time.

#### Allowed Props

| Attr | Description |
| ---- | ----------- |
| `format` |**optional**:  the format of the date and time using [Microsoft Standard Date/Time Format](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings) or [Microsoft Custom Date/Time Format](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings). |

#### Examples

Using a date and time of 3/23/1978 11:30:00 am

##### Template 1
```xml
This is the date and time <timestamp />
```
##### Output 1
```
This is the date and time 3/23/1978 11:30:00
```

##### Template 2
```xml
This is the date and time <timestamp format="MM-dd-yy"/>
```
##### Output 2
```
This is the date and time 03-23-78
```


##### Template 3
```xml
This is the date and time <timestamp format="MM-dd-yyyy hh:mm tt"/>
```
##### Output 3
```
This is the date and time 03-23-1778 11:30 AM
```

<br>
<br>

## Tag: `<version>`

`<version>` tag allows you to print the current version of the templating library.

#### Allowed Props

| Attr | Description |
| ---- | ----------- |
| `prefix` | **optional**: The value of this attribute will be prepended to the version.  Default value is: "Templating Engine Version " |
| `exp` | **optional**: Allows you to modify the value before writing it to the output stream. a javascript expression that returns a string.  The last expression evaluated is what will be returned.  |

#### Examples

Using a version of 1.2.3

##### Template 1
```xml
This version is <version />
```
##### Output 1
```
This version is 1.2.3
```
##### Template 2
```xml
This version is <version prefix="TE:" />
```
##### Output 2
```
This version is TE:1.2.3
```
##### Template 3
```xml
This version is <version prefix="TE:" exp="x1.substr(0,6)"/>
```
##### Output 3
```
This version is TE:1.2
```

<br>
<br>
<br>

## Notes about Javascript Expressions

In a number of tags you can use javascript to manipulate data and return a value.  The `<if>`, `<column>`, and `<var>` 
tags are examples.  These expressions are all evaluated the same and the following rules apply.

#### Expression evaluation

Your expression can be as long as you want, and you can have as many expressions in the line as you want.  

The last expression evaluated is always the one returned.
```
<var:var1 exp="x1 = x1 + 3;  x1 = x1+5"/>
```
will return a value of 10 if the initial value of the variable `var1` was 2, because 2 + 3 + 5 == 10.

##### Variable Names

If there is a single value then the variable storing the value in the expression will be `x1`.

If there are more than one value, then the variables will be `x1`, `x2`, `x3`, etc... counting up.

As an example, for a table with 5 columns, the following is a valid example.

```
<table where="x1 == 1 || x2 == 3 || x5 == 10"></table>
```

*Variable names do not need to present in an expression.*

`x1` is always the first value, `x2` the second, and so on, whatever order they appear in the expression. Using an `xN` with no matching value is an error. A value that reads as a number, such as `42` or `-1.5`, is a number in the expression; anything else is a string.

##### Supported syntax

Expressions use a subset of JavaScript, evaluated with JavaScript's rules for comparing and converting values.

| Feature | Syntax |
| ------- | ------ |
| Literals | numbers, `'single'` or `"double"` quoted strings, `true`, `false`, `null`, `undefined` |
| Arithmetic | `+ - * / %`, with `+` joining strings when either side is a string |
| Comparison | `== != === !== < > <= >=` |
| Logic | `&& \|\| !` and `test ? a : b` |
| Assignment | `= += -= *= /= %=`, and `++`/`--` before or after a variable |
| Statements | separated by `;`, and the last value is the result |
| String functions | `length`, `substr`, `substring`, `slice`, `indexOf`, `lastIndexOf`, `includes`, `startsWith`, `endsWith`, `charAt`, `toLowerCase`, `toUpperCase`, `trim`, `trimStart`, `trimEnd`, `replace` (first match, plain text), `padStart`, `padEnd` |
| Number functions | `toFixed`, `toString` |

Loops, functions, objects, arrays, and regular expressions are not supported, and an expression that uses them is an error. Because there are no loops, every expression finishes.
