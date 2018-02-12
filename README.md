# OsSql

# The Story
The library was developed by Amit (Amit_B) Barami and Dan (Agresiv) Elimelech in order to assist data-based projects in order to save variable data into a database.
The idea was to make it very simple to use and at the same time useful.
We later discovered more modern methods to do the same idea, such as Entity Framework.
This class was however a great practicing opportunity for us, and we still is it in some of our projects!
Full documentation can be found as standart XML documentation along with the release, as well as in the repository wiki. Note that the wiki is auto-generated using [MarkdownGenerator](https://github.com/neuecc/MarkdownGenerator).

# Advantages & Features
* Easy and simple to use
* Easily connect and use database queries with OsSql.SQL class
* Use table & column objects rather than names, which makes query table name mistakes impossible
* Manipulate database structure (columns) with AddTable(), AddColumn(), UpdateStructure() & more column methods
* Can use AutoSelect() on a table to automatically select only data shared between the code and the database, as same goes with AutoInsert() and AutoUpdate()
* Mainly supports MySQL; also supports most regular data types, as well as extra ones (which aren't defined by default) such as DateTime as unix time, Json objects, and even custom data based on a class object
* Easily share data in class object with the database with easy and modern methods, like attributes

# Disadvantages
* Since we created this in a hurry and tried to make it most simple, we didn't researched everything to its end, so the library may not support any data type or database type
* Not really compatible for large-scale projects
