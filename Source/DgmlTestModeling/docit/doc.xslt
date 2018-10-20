<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns="http://www.w3.org/1999/xhtml">
  <xsl:output method="html" indent="yes" />
  <xsl:template match="/">
    <html>
      <style type="text/css">
        h1 { color:rgb(0,114,188); border-bottom:1 solid black;}
        h2 { color:rgb(0,114,188); border-bottom:1 solid black; }
        .keyword { color: blue; font-family:courier new;font-size:10pt; }
        .type { color:rgb(43,145,175); font-family:courier new;font-size:10pt; }
        .typelink { color:rgb(43,145,175); font-family:courier new;font-size:10pt;}
        .delim { font-family:courier new;font-size:10pt; color:black; }
        .paramtable { margin-top:0; padding-top:0;}
        .border { border:2 dashed #CCDDEE; padding:5px; }
        .headinglink { color:rgb(0,114,188); border-bottom:1 solid black; }
        body, td, th { font-family:verdana; font-size:10pt; }
        pre { font-family:courier new; font-size:10pt; }
      </style>
      <body>
        <h1>
          <xsl:value-of select="doc/assembly/name" />
        </h1>
        <xsl:apply-templates select="doc/assembly/*" mode="toc" />
        <xsl:apply-templates select="doc/assembly/*" />
      </body>
    </html>
  </xsl:template>
  <xsl:template match="class | interface" mode="toc">
<a name="TOC{generate-id(.)}" class="typelink">

    <xsl:if test="@public">
      <span class="keyword">public</span> </xsl:if>
    <xsl:if test="@protected">
      <span class="keyword">protected</span> </xsl:if>
    <xsl:if test="@abstract">
      <span class="keyword">abstract</span> </xsl:if>
    <span class="keyword">
      <xsl:value-of select="name()" />
    </span> <span class="type"><a href="#{generate-id(.)}" class="typelink"><xsl:value-of select="@name" /></a></span> <xsl:call-template name="extendstoc" /><span class="delim">{</span><br /><div style="margin-left:2em"><xsl:apply-templates select="*" mode="toc" />
       
    </div><span class="delim">}</span><br /><br /></a></xsl:template>
  <xsl:template name="extendstoc">
    <xsl:for-each select="extends | implements">
      <xsl:if test="position() = 1"> : </xsl:if>
      <xsl:call-template name="linktype">
        <xsl:with-param name="name" select="@name" />
      </xsl:call-template>
      <xsl:if test="position() != last()">, </xsl:if>
      <xsl:if test="position() = last()"> </xsl:if>
    </xsl:for-each>
  </xsl:template>
  <xsl:template name="extendsheading">
    <xsl:for-each select="extends | implements">
      <xsl:if test="position() = 1"> : </xsl:if>
      <xsl:call-template name="linkheading">
        <xsl:with-param name="name" select="@name" />
      </xsl:call-template>
      <xsl:if test="position() != last()">, </xsl:if>
      <xsl:if test="position() = last()"> </xsl:if>
    </xsl:for-each>
  </xsl:template>
  <xsl:template match="enum" mode="toc">
    <xsl:if test="@public">
      <span class="keyword">public</span> 
    </xsl:if>
    <xsl:if test="@protected">
      <span class="keyword">protected</span> 
    </xsl:if>
    <span class="keyword">enum</span> 
    <span class="type"><a href="#{generate-id(.)}" class="typelink"><xsl:value-of select="@name" /></a></span> <span class="delim">{</span><br /><div style="margin-left:2em"><xsl:for-each select="field"><span class="type"><a href="#{generate-id(.)}" class="typelink"><xsl:value-of select="@name" /></a>  = <xsl:value-of select="@value" /><span class="delim">;</span><xsl:if test="position() != last()"><br /></xsl:if></span></xsl:for-each> 
    </div><span class="delim">}</span><br /><br /></xsl:template>
  <xsl:template match="delegate" mode="toc">
    <xsl:if test="@public">
      <span class="keyword">public</span> 
    </xsl:if>
    <xsl:if test="@protected">
      <span class="keyword">protected</span> 
    </xsl:if>
    <span class="keyword">delegate void</span> 
    <span class="type"><a href="#{generate-id(.)}" class="typelink"><xsl:value-of select="@name" /></a></span> <span class="delim">(</span><xsl:for-each select="params/arg"><xsl:call-template name="linktype"><xsl:with-param name="name" select="@name" /></xsl:call-template> <span class="delim"><xsl:value-of select="@var" /></span><xsl:if test="position() != last()"><span class="delim">, </span></xsl:if></xsl:for-each><span class="delim">);</span><br /><br /></xsl:template>
  <xsl:template name="linktype">
    <xsl:param name="name" />
    <xsl:choose>
      <xsl:when test="/doc/assembly/*[@name=$name]">
        <xsl:for-each select="/doc/assembly/*[@name=$name]">
          <a href="#{generate-id(.)}" class="typelink">
            <xsl:value-of select="$name" />
          </a>
        </xsl:for-each>
      </xsl:when>
      <xsl:otherwise>
        <span class="type">
          <xsl:value-of select="$name" />
        </span>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <xsl:template name="linkheading">
    <xsl:param name="name" />
    <xsl:choose>
      <xsl:when test="/doc/assembly/*[@name=$name]">
        <xsl:for-each select="/doc/assembly/*[@name=$name]">
          <a href="#{generate-id(.)}" class="headinglink">
            <xsl:value-of select="$name" />
          </a>
        </xsl:for-each>
      </xsl:when>
      <xsl:otherwise>
        <span class="headinglink">
          <xsl:value-of select="$name" />
        </span>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="field" mode="toc">
    <xsl:if test="@public">
      <span class="keyword">public</span> </xsl:if>
    <xsl:if test="@static">
      <span class="keyword">static</span> </xsl:if>
    <xsl:if test="@protected">
      <span class="keyword">protected</span> </xsl:if>
    <span class="type">
      <a href="#{generate-id(.)}" class="typelink">
        <xsl:value-of select="@name" />
      </a>
      <span class="delim">;</span>
      <xsl:if test="position() != last()">
        <br />
      </xsl:if>
    </span>
  </xsl:template>
  <xsl:template match="event" mode="toc">
    <xsl:if test="@public">
      <span class="keyword">public</span> </xsl:if>
    <xsl:if test="@protected">
      <span class="keyword">protected</span> </xsl:if>
    <span class="keyword">event</span> <span class="type"><xsl:value-of select="type/@name" />
       <a href="#{generate-id(.)}" class="delim"><span class="delim"><xsl:value-of select="@name" />;
        </span></a><xsl:if test="position() != last()"><br /></xsl:if></span></xsl:template>
  <xsl:template match="property" mode="toc">
    <xsl:if test="@public">
      <span class="keyword">public</span> </xsl:if>
    <xsl:if test="@protected">
      <span class="keyword">protected</span> </xsl:if>
    <xsl:if test="@virtual">
      <span class="keyword">virtual</span> </xsl:if>
    <xsl:if test="@abstract">
      <span class="keyword">abstract</span> </xsl:if>
    <xsl:call-template name="linktype">
      <xsl:with-param name="name" select="type/@name" />
    </xsl:call-template> 
    <span class="delim"><a href="#{generate-id(.)}" class="delim"><xsl:value-of select="@name" /></a></span> <xsl:if test="arg">[<xsl:for-each select="arg"><xsl:value-of select="@name" /><xsl:if test="position() != last()">,</xsl:if></xsl:for-each>] </xsl:if><span class="delim">{</span> <xsl:if test="@get"><span class="keyword">get</span><span class="delim">;</span> 
    </xsl:if><xsl:if test="@set"><span class="keyword">set</span><span class="delim">;</span> 
    </xsl:if><span class="delim">}</span><xsl:if test="position() != last()"><br /></xsl:if></xsl:template>
  <xsl:template match="method" mode="toc">
    <xsl:if test="@public">
      <span class="keyword">public</span> </xsl:if>
    <xsl:if test="@protected">
      <span class="keyword">protected</span> </xsl:if>
    <xsl:if test="@virtual">
      <span class="keyword">virtual</span> </xsl:if>
    <xsl:if test="@abstract">
      <span class="keyword">abstract</span> </xsl:if>
    <xsl:if test="returns/@name">
      <xsl:call-template name="linktype">
        <xsl:with-param name="name" select="returns/@name" />
      </xsl:call-template> </xsl:if>
    <span class="type">
      <a href="#{generate-id(.)}" class="delim">
        <xsl:choose>
          <xsl:when test="@class">
            <xsl:value-of select="@class" />
          </xsl:when>
          <!-- constructor-->
          <xsl:otherwise>
            <xsl:value-of select="@name" />
          </xsl:otherwise>
        </xsl:choose>
      </a>
    </span>
    <span class="delim">(</span>
    <xsl:for-each select="params/arg">
      <xsl:call-template name="linktype">
        <xsl:with-param name="name" select="@name" />
      </xsl:call-template> <span class="delim"><xsl:value-of select="@var" /></span><xsl:if test="position() != last()"><span class="delim">, </span></xsl:if></xsl:for-each>
    <span class="delim">);</span>
    <xsl:if test="position() != last()">
      <br />
    </xsl:if>
  </xsl:template>
  <!-- ========================== details ========================== -->
  <xsl:template match="class | interface">
    <h2>
      <a name="{generate-id(.)}">
        <xsl:if test="@abstract"> abstract </xsl:if>
        <xsl:value-of select="name()" /> <xsl:value-of select="@name" /></a>
      <xsl:call-template name="extendsheading" />
    </h2>
    <div style="margin-left:1em">
      <xsl:apply-templates select="*" />
       
    </div>
  </xsl:template>
  <xsl:template match="summary">
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>
  <xsl:template match="value">
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>
  <xsl:template match="example">
    <p>
      <b>Example:</b>
      <br />
      <xsl:apply-templates />
    </p>
  </xsl:template>
  <xsl:template match="remarks">
    <p>
      <b>Remarks:</b>
      <br />
      <xsl:apply-templates />
    </p>
  </xsl:template>
  <xsl:template match="exception"></xsl:template>
  <xsl:template match="code">
    <pre>
      <xsl:value-of select="." disable-output-escaping="yes" />
    </pre>
  </xsl:template>
  <xsl:template match="returns">
    <p>
      <b>Returns:</b>
      <br />
      <xsl:value-of select="." />
    </p>
  </xsl:template>
  <xsl:template match="threading">
    <p>
      <b>Threading:</b>
      <br />
      <xsl:apply-templates />
    </p>
  </xsl:template>
  <xsl:template match="params">
    <xsl:call-template name="showparams" />
  </xsl:template>
  <xsl:template name="showparams">
    <b>Parameters:</b>
    <div style="margin-left:2em">
      <table cellpadding="3" class="paramtable">
        <xsl:for-each select="arg">
          <tr>
            <td valign="top">
              <xsl:call-template name="linktype">
                <xsl:with-param name="name" select="@name" />
              </xsl:call-template>
            </td>
            <td width="20"></td>
            <td valign="top">
              <i>
                <xsl:value-of select="@var" />
              </i>
            </td>
            <td width="20"></td>
            <td valign="top">
              <xsl:value-of select="." />
            </td>
          </tr>
        </xsl:for-each>        
      </table>
    </div>
  </xsl:template>
  <xsl:template match="enum">
    <xsl:variable name="enum" select="@name" />
    <h2>
      <a name="{generate-id(.)}">
        enum <xsl:value-of select="@name" /></a>
    </h2>
    <div style="margin-left:1em">
      <xsl:apply-templates select="summary" />
      <table cellpadding="3" border="2">
        <xsl:for-each select="field">
          <tr>
            <td valign="top">
              <span class="type">
                <a name="{generate-id(.)}">
                  <xsl:value-of select="@name" />
                </a>
              </span>
            </td>
            <td valign="top" width="30">
              <xsl:value-of select="@value" />
            </td>
            <td valign="top">
              <xsl:value-of select="summary" />
            </td>
          </tr>
        </xsl:for-each>       
      </table>
    </div>
  </xsl:template>
  <xsl:template match="method">
    <div class="border">
      <xsl:if test="@public">
        <span class="keyword">public</span> </xsl:if>
      <xsl:if test="@protected">
        <span class="keyword">protected</span> </xsl:if>
      <xsl:if test="@virtual">
        <span class="keyword">virtual</span> </xsl:if>
      <xsl:if test="@abstract">
        <span class="keyword">abstract</span> </xsl:if>
      <xsl:if test="returns/@name">
        <xsl:call-template name="linktype">
          <xsl:with-param name="name" select="returns/@name" />
        </xsl:call-template> </xsl:if>
      <span class="delim">
        <a name="{generate-id(.)}" class="delim">
          <xsl:choose>
            <xsl:when test="@class">
              <xsl:value-of select="@class" />
            </xsl:when>
            <!-- constructor-->
            <xsl:otherwise>
              <xsl:value-of select="@name" />
            </xsl:otherwise>
          </xsl:choose>
        </a>
      </span>
      <span class="delim">(</span>
      <xsl:for-each select="params/arg">
        <xsl:call-template name="linktype">
          <xsl:with-param name="name" select="@name" />
        </xsl:call-template> <span class="delim"><xsl:value-of select="@var" /><xsl:if test="position()!=last()">, </xsl:if></span></xsl:for-each>
      <span class="delim">)</span>
      <br />
      <br />
      <xsl:call-template name="typeparams" />
      <xsl:apply-templates select="*" />
      <xsl:call-template name="permissions" />
      <xsl:call-template name="exceptions" />
    </div>
    <br />
  </xsl:template>
  <xsl:template match="delegate">
    <xsl:variable name="delegate" select="@name" />
    <h2>delegate void <a name="{generate-id(.)}"><xsl:value-of select="@name" /></a></h2>
    <div class="border">
      <xsl:call-template name="typeparams" />
      <xsl:apply-templates select="*" />
      <xsl:call-template name="permissions" />
      <xsl:call-template name="exceptions" />
    </div>
    <br />
  </xsl:template>
  <xsl:template match="property">
    <div class="border">
      <xsl:if test="@public">
        <span class="keyword">public</span> </xsl:if>
      <xsl:if test="@protected">
        <span class="keyword">protected</span> </xsl:if>
      <xsl:if test="@virtual">
        <span class="keyword">virtual</span> </xsl:if>
      <xsl:if test="@abstract">
        <span class="keyword">abstract</span> </xsl:if>
      <xsl:call-template name="linktype">
        <xsl:with-param name="name" select="type/@name" />
      </xsl:call-template> <span class="delim"><a name="{generate-id(.)}" class="delim"><xsl:value-of select="@name" /></a></span> <xsl:if test="arg">[<xsl:for-each select="arg"><xsl:value-of select="@name" /><xsl:if test="position() != last()">,</xsl:if></xsl:for-each>] </xsl:if><span class="delim">{</span><xsl:if test="@get">
         
        <span class="keyword">get</span><span class="delim">;</span> 
      </xsl:if><xsl:if test="@set"><span class="keyword">set</span><span class="delim">;</span> 
      </xsl:if><span class="delim">}</span><br /><xsl:apply-templates select="*" /><xsl:if test="arg"><xsl:call-template name="showparams" /></xsl:if><xsl:call-template name="permissions" /><xsl:call-template name="exceptions" /></div>
    <br />
  </xsl:template>
  <xsl:template match="field">
    <div class="border">
      <xsl:if test="@public">
        <span class="keyword">public</span> </xsl:if>
      <xsl:if test="@protected">
        <span class="keyword">protected</span> </xsl:if>
      <xsl:if test="@static">
        <span class="keyword">static</span> </xsl:if>
      <xsl:call-template name="linktype">
        <xsl:with-param name="name" select="type/@name" />
      </xsl:call-template> <span class="delim"><a name="{generate-id(.)}" class="delim"><xsl:value-of select="@name" /></a></span> 
      <br /><xsl:apply-templates select="*" /><xsl:call-template name="permissions" /><xsl:call-template name="exceptions" /></div>
    <br />
  </xsl:template>
  <xsl:template match="event">
    <div class="border">
      <xsl:if test="@public">
        <span class="keyword">public</span> </xsl:if>
      <xsl:if test="@protected">
        <span class="keyword">protected</span> </xsl:if>
      <xsl:call-template name="linktype">
        <xsl:with-param name="name" select="type/@name" />
      </xsl:call-template> <span class="delim"><a name="{generate-id(.)}" class="delim"><xsl:value-of select="@name" /></a></span> 
      <br /><xsl:apply-templates select="*" /></div>
    <br />
  </xsl:template>
  <xsl:template match="list">
    <xsl:choose>
      <xsl:when test="@type = 'number'">
        <ol>
          <xsl:apply-templates />
        </ol>
      </xsl:when>
      <xsl:when test="@type = 'bullet'">
        <ul>
          <xsl:apply-templates />
        </ul>
      </xsl:when>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="list[@type='table']">
    <table border="2">
      <xsl:if test="listheader">
        <tr>
          <th>
            <xsl:value-of select="listheader/term" />
          </th>
          <th>
            <xsl:value-of select="listheader/description" />
          </th>
        </tr>
      </xsl:if>
      <xsl:for-each select="item">
        <tr>
          <td>
            <xsl:apply-templates select="term" />
          </td>
          <td>
            <xsl:apply-templates select="description" />
          </td>
        </tr>
      </xsl:for-each>
    </table>
  </xsl:template>
  <xsl:template match="item">
    <li>
      <xsl:apply-templates />
    </li>
  </xsl:template>
  <xsl:template match="para">
    <p>
      <xsl:apply-templates />
    </p>
  </xsl:template>
  <xsl:template match="see">
    <p>
      See <a href="#{@cref}"><xsl:value-of select="@cref" /></a></p>
  </xsl:template>
  <xsl:template name="exceptions">
    <xsl:if test="exception">
      <p>
        <b>Exceptions:</b>
        <div style="margin-left:1em">
          <table border="2" cellpadding="3">
            <xsl:for-each select="exception">
              <tr>
                <th>
                  <xsl:value-of select="@cref" />
                </th>
                <td>
                  <xsl:value-of select="." />
                </td>
              </tr>
            </xsl:for-each>
          </table>
        </div>
      </p>
    </xsl:if>
  </xsl:template>
  <xsl:template name="permissions">
    <xsl:if test="permission">
      <p>
        <b>Permissions:</b>
        <div style="margin-left:1em">
          <table border="2" cellpadding="3">
            <xsl:for-each select="permission">
              <tr>
                <th>
                  <xsl:value-of select="@cref" />
                </th>
                <td>
                  <xsl:value-of select="." />
                </td>
              </tr>
            </xsl:for-each>
          </table>
        </div>
      </p>
    </xsl:if>
  </xsl:template>
  <xsl:template name="typeparams">
    <xsl:if test="typeparam">
      <table cellpadding="3" class="paramtable" style="margin-left:2em">
        <xsl:for-each select="typeparam">
          <tr>
            <td valign="top">
              <xsl:call-template name="linktype">
                <xsl:with-param name="name" select="@name" />
              </xsl:call-template>
            </td>
            <td width="20"></td>
            <td width="20"></td>
            <td valign="top">
              <xsl:value-of select="." />
            </td>
          </tr>
        </xsl:for-each> 
      </table>
    </xsl:if>
  </xsl:template>
  <xsl:template match="b">
    <b>
      <xsl:apply-templates />
    </b>
  </xsl:template>
  <xsl:template match="paramref">
    <xsl:value-of select="@name" />
  </xsl:template>
  <xsl:template match="typeparamref ">
    <xsl:value-of select="@name" />
  </xsl:template>
  <xsl:template match="name"></xsl:template>
  <xsl:template match="typeparam"></xsl:template>
  <xsl:template match="name" mode="toc"></xsl:template>
  <xsl:template match="summary" mode="toc"></xsl:template>
  <xsl:template match="returns" mode="toc"></xsl:template>
  <xsl:template match="param" mode="toc"></xsl:template>
  <xsl:template match="example" mode="toc"></xsl:template>
  <xsl:template match="remarks" mode="toc"></xsl:template>
  <xsl:template match="threading" mode="toc"></xsl:template>
  <xsl:template match="see" mode="toc"></xsl:template>
  <xsl:template match="exception" mode="toc"></xsl:template>
  <xsl:template match="permission" mode="toc"></xsl:template>
  <xsl:template match="value" mode="toc"></xsl:template>
  <xsl:template match="seealso" mode="toc"></xsl:template>
  <xsl:template match="typeparam" mode="toc"></xsl:template>
  <xsl:template match="arg"></xsl:template>
</xsl:stylesheet>