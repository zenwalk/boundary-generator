This is a simple geoprocessing tool that extracts adjacency information from a layer of polygons. The output is a polyline layer with one record for each border between each pair of neighboring polygons.

[Download v. 0.1](http://boundary-generator.googlecode.com/files/boundarygen_alpha.exe).

After downloading and installing, you can right-click on a new toolbox, choose "Add -> Tool", and select Boundary Generator under the "MWSW Geoprocessing Tools" group. You can also find MWSW Boundary Generator on the "Index" toolbox view.

Highly recommended: Send a blank email to [mwsw-prod-announce-subscribe@googlegroups.com](mailto:mwsw-prod-announce-subscribe@googlegroups.com) to receive announcements about this and other projects. (You can also sign up [via the web](http://groups.google.com/group/mwsw-prod-announce)). Your email address will never be shared with anyone.

Project status (alpha):

  * Generating boundaries works; initial spot checks are giving correct answers.
  * Some of the Geoprocessing UI behavior isn't quite finished -- e.g., you must specify the output location yourself; the tool gives an erroneous validation error if you try to run it on a layer that isn't in your TOC. (Expect these to be fixed soon.)
  * The tool hasn't really been tested in a model builder / python scripting framework yet, and needs some adjustments to work well there.

Project rationale:

  * It is a good demonstration project for custom programming geoprocessing tools in C#, as well as simple computational geometry & spatial indexing techniques.
  * It is robust and geometry based; the other extensions I've come across for doing the same thing simply searched for line segments with identical endpoints (and would thus fail with partial overlaps, T junctions, numeric precision issues, and the like) or used polygon-polygon intersection to come up with polygon adjacency lists (but not accurate border lines), as described in [this old article](http://www.esri.com/news/arcuser/0401/topo.html).
  * This functionality is available without an ArcInfo license. (If you do have ArcInfo, you can use the offical [Polygon To Line](http://webhelp.esri.com/arcgisdesktop/9.2/index.cfm?TopicName=Polygon_To_Line_(Data_Management)) tool. I would be curious to see the results of a side-by-side comparison from someone with a license...)

If you are still using ArcView 3.x, my (dated and not well-tested; beware!) Avenue script for doing the same thing is available at http://arcscripts.esri.com/details.asp?dbid=12786.
