using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using g3;

namespace f3
{
    public class XMLInputStream : IInputStream
    {
        public XmlDocument xml { get; set; }

        public event InputStream_AttributeHandler OnAttribute;
        public event InputStream_NodeHandler OnBeginScene;
        public event InputStream_NodeHandler OnBeginSceneObject;
        public event InputStream_NodeHandler OnEndScene;
        public event InputStream_NodeHandler OnEndSceneObject;

        public void Restore()
        {
            XmlNodeList scenes = xml.SelectNodes("/" + IOStrings.Scene);
            foreach ( XmlNode scene in scenes ) {
                if ( OnBeginScene != null )
                    OnBeginScene();


                XmlNodeList sceneObjects = scene.SelectNodes(IOStrings.SceneObject);
                foreach ( XmlNode so in sceneObjects ) {
                    if (OnBeginSceneObject != null)
                        OnBeginSceneObject();

                    foreach ( XmlNode a in so.ChildNodes ) {
                        OnAttribute(a.Name, a.InnerText);
                    }

                    if (OnEndSceneObject != null)
                        OnEndSceneObject();
                }


                if (OnEndScene != null)
                    OnEndScene();
            }
        }
    }
}
