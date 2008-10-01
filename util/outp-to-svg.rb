#!/usr/bin/env ruby

# Simple dumper that converts the output from PMQTreeTest::DumpIndex
#  into an SVG file.

class Dumpee 
  def initialize()
    @xmin = (1.0/0)
    @ymin = (1.0/0)
    @xmax = (-1.0/0)
    @ymax = (-1.0/0)
    @quads = []
    @lines = []
  end

  def node(idx,quad)
    if (idx > 0)
      @xmin = [quad[0],@xmin].min
      @ymin = [quad[1],@ymin].min
      @xmax = [quad[2],@xmax].max
      @ymax = [quad[3],@ymax].max
    end
    w = quad[2] - quad[0];
    h = quad[3] - quad[1];
    q = <<EOF
  <rect x="#{quad[0]}" y="#{quad[1]}" width="#{w}" height="#{h}" class="lvl#{idx}"/>
EOF
    @quads.push(q)
  end

  def line(l)
    st = l[0]
    nd = l[1]
    q = <<EOF
  <line x1="#{st[0]}" y1="#{st[1]}" x2="#{nd[0]}" y2="#{nd[1]}"/>
EOF
    @lines.push(q)
  end
  
  def dodump
    r = <<EOF
<?xml version='1.0'?>
<!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" 
"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">

<svg width="600px" height="600px" version="1.1"
     xmlns="http://www.w3.org/2000/svg"
     viewBox="#{@xmin} #{@ymin} #{@xmax - @xmin} #{@ymax - @ymin}">
<defs><style type="text/css"><![CDATA[
* { stroke-width:0.5%; stroke: black ; fill : none }
line { stroke : red }
rect.lvl2 { stroke : blue }
rect.lvl3 { stroke : green }
rect.lvl4 { stroke : purple }
rect.lvl5 { stroke : black }
]]></style></defs>
#{@quads.join('')}
#{@lines.join('')}
</svg>
EOF
    r
  end
end

$d = Dumpee.new
$stdin.each_line do |l|
  r = l.match(/DUMP:(.*)$/)
  if (!r.nil?)
    #    puts r[1].strip
    $d.instance_eval(r[1].strip)
  end
end

puts $d.dodump
