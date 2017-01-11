using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Text;
using g3;

namespace f3
{
    public class XMLOutputStream : IOutputStream
    {
        public XmlWriter Writer { get; set; }

        public XMLOutputStream(XmlWriter writer)
        {
            Writer = writer;
        }


        public void BeginScene(string sVersion)
        {
            if (Writer == null)
                throw new InvalidOperationException("XMLOutputStream.BeginScene: xml Writer is not initialized!!");

            Writer.WriteStartElement(IOStrings.Scene);
                Writer.WriteAttributeString(IOStrings.SceneVersion, sVersion);
        }

        public void EndScene()
        {
            Writer.WriteEndElement();
        }

        public void BeginSceneObject()
        {
            Writer.WriteStartElement(IOStrings.SceneObject);
        }

        public void EndSceneObject()
        {
            Writer.WriteEndElement();
        }


        public void BeginStruct(string sType, string sIdentifier)
        {
            Writer.WriteStartElement(IOStrings.Struct);
            Writer.WriteAttributeString(IOStrings.StructType, sType);
            if ( sIdentifier.Length > 0 )
                Writer.WriteAttributeString(IOStrings.StructIdentifier, sIdentifier);
        }
        public void EndStruct()
        {
            Writer.WriteEndElement();
        }


        public void AddAttribute(string sName, string sValue, bool bInline = false)
        {
            if ( bInline )
                Writer.WriteAttributeString(sName, sValue);
            else
                Writer.WriteElementString(sName, sValue);
        }

        public void AddAttribute(string sName, int nValue, bool bInline = false)
        {
            if ( bInline )
                Writer.WriteAttributeString(sName, nValue.ToString());
            else
                Writer.WriteElementString(sName, nValue.ToString());
        }

        public void AddAttribute(string sName, bool bValue, bool bInline = false)
        {
            if ( bInline )
                Writer.WriteAttributeString(sName, bValue ? "true" : "false");
            else
                Writer.WriteElementString(sName, bValue ? "true" : "false");
        }

        public void AddAttribute(string sName, Vector2f vValue, bool bInline = false)
        {
            string s = vValue[0].ToString("F8", CultureInfo.InvariantCulture) + " "
                + vValue[1].ToString("F8", CultureInfo.InvariantCulture);
            if (bInline)
                Writer.WriteAttributeString(sName, s);
            else
                Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, Vector3f vValue, bool bInline = false)
        {
            string s = vValue[0].ToString("F8", CultureInfo.InvariantCulture) + " "
                + vValue[1].ToString("F8", CultureInfo.InvariantCulture) + " "
                + vValue[2].ToString("F8", CultureInfo.InvariantCulture);
            if (bInline)
                Writer.WriteAttributeString(sName, s);
            else
                Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, float fValue, bool bInline = false)
        {
            if (bInline)
                Writer.WriteAttributeString(sName, fValue.ToString("F8", CultureInfo.InvariantCulture));
            else
                Writer.WriteElementString(sName, fValue.ToString("F8", CultureInfo.InvariantCulture));
        }

        public void AddAttribute(string sName, Quaternionf qValue, bool bInline = false)
        {
            string s = qValue[0].ToString("F8", CultureInfo.InvariantCulture) + " "
                + qValue[1].ToString("F8", CultureInfo.InvariantCulture) + " "
                + qValue[2].ToString("F8", CultureInfo.InvariantCulture) + " "
                + qValue[3].ToString("F8", CultureInfo.InvariantCulture);
            if (bInline)
                Writer.WriteAttributeString(sName, s);
            else
                Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, Colorf cValue, bool bInline = false)
        {
            string s = cValue[0].ToString("F8", CultureInfo.InvariantCulture) + " "
                + cValue[1].ToString("F8", CultureInfo.InvariantCulture) + " "
                + cValue[2].ToString("F8", CultureInfo.InvariantCulture) + " "
                + cValue[3].ToString("F8", CultureInfo.InvariantCulture);
            if (bInline)
                Writer.WriteAttributeString(sName, s);
            else
                Writer.WriteElementString(sName, s);
        }


        public void AddAttribute(string sName, IEnumerable<Vector3d> vVertices) {
            string s = "";
            int i = 0;
            foreach (Vector3d v in vVertices) { 
                if (i++ > 0)
                    s += " ";
                s += v[0].ToString("F8", CultureInfo.InvariantCulture) + " " +
                    v[1].ToString("F8", CultureInfo.InvariantCulture) + " " +
                    v[2].ToString("F8", CultureInfo.InvariantCulture);
            }
            Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, IEnumerable<Vector3f> vVertices)
        {
            string s = "";
            int i = 0;
            foreach (Vector3f v in vVertices) {
                if (i++ > 0)
                    s += " ";
                s += v[0].ToString("F8", CultureInfo.InvariantCulture) + " " +
                    v[1].ToString("F8", CultureInfo.InvariantCulture) + " " +
                    v[2].ToString("F8", CultureInfo.InvariantCulture);
            }
            Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, IEnumerable<Vector3i> vVertices)
        {
            string s = "";
            int i = 0;
            foreach (Vector3i v in vVertices) {
                if (i++ > 0)
                    s += " ";
                s += v[0].ToString() + " " + v[1].ToString() + " " + v[2].ToString();
            }
            Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, IEnumerable<Index3i> vVertices)
        {
            string s = "";
            int i = 0;
            foreach (Index3i v in vVertices) {
                if (i++ > 0)
                    s += " ";
                s += v[0].ToString() + " " + v[1].ToString() + " " + v[2].ToString();
            }
            Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, IEnumerable<Vector2d> vVertices)
        {
            string s = "";
            int i = 0;
            foreach (Vector2d v in vVertices) {
                if (i++ > 0)
                    s += " ";
                s += v[0].ToString("F8", CultureInfo.InvariantCulture) + " " +
                    v[1].ToString("F8", CultureInfo.InvariantCulture);
            }
            Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, IEnumerable<Vector2f> vVertices)
        {
            string s = "";
            int i = 0;
            foreach (Vector2f v in vVertices) {
                if (i++ > 0)
                    s += " ";
                s += v[0].ToString("F8", CultureInfo.InvariantCulture) + " " +
                    v[1].ToString("F8", CultureInfo.InvariantCulture);
            }
            Writer.WriteElementString(sName, s);
        }

        public void AddAttribute(string sName, byte[] buffer)
        {
            Writer.WriteElementString(sName,
                System.Convert.ToBase64String(buffer));
        }

    }
}
