--------------------------------------------------
Testing order of keys ...
--------------------------------------------------

01
02
03
04
05
06
07
08
09
10
resource.loader
file.resource.loader.class
file.resource.loader.description
file.resource.loader.path
classpath.resource.loader.class
classpath.resource.loader.description
datasource.resource.loader.class
datasource.resource.loader.description
logger.type
config.string.value
config.boolean.value
config.byte.value
config.short.value
config.int.value
config.long.value
config.float.value
config.double.value
escape.comma1
escape.comma2
include1.property
include2.property

--------------------------------------------------
Testing retrieval of CSV values ...
--------------------------------------------------

file
classpath
datasource

--------------------------------------------------
Testing subset(prefix).getKeys() ...
--------------------------------------------------

class
description
path

--------------------------------------------------
Testing getVector(prefix) ...
--------------------------------------------------

/path01
/path02
/path03

--------------------------------------------------
Testing getString(key) ...
--------------------------------------------------

string

--------------------------------------------------
Testing getBoolean(key) ...
--------------------------------------------------

True

--------------------------------------------------
Testing getByte(key) ...
--------------------------------------------------

1

--------------------------------------------------
Testing getShort(key) ...
--------------------------------------------------

1

--------------------------------------------------
Testing getInt(key) ...
--------------------------------------------------

30000

--------------------------------------------------
Testing getLong(key) ...
--------------------------------------------------

1000000

--------------------------------------------------
Testing getFloat(key) ...
--------------------------------------------------

3.14

--------------------------------------------------
Testing getDouble(key) ...
--------------------------------------------------

3.14159265358793

--------------------------------------------------
Testing escaped-comma scalar...
--------------------------------------------------

foo,

--------------------------------------------------
Testing escaped-comma vector...
--------------------------------------------------

bar,lala
woogie,bjork!



