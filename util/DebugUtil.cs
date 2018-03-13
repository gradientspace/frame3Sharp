using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using g3;

namespace f3
{
	public class DebugUtil
	{
        protected DebugUtil ()
		{
		}

        static int __log_level = 0;
        static bool __show_thread_id = false;

        static public int LogLevel
        {
            get { return __log_level; }
            set { __log_level = value; }
        }

        static public bool ShowThreadID {
            get { return __show_thread_id; }
            set { __show_thread_id = value; }
        }

        static string log_prefix {
            get {
                if (__show_thread_id)
                    return System.Threading.Thread.CurrentThread.ManagedThreadId.ToString() + ": ";
                else return "";
            }
        }

        // Log prints if the nLevel param is <= LogLevel
        static public void Log(int nLevel, string sMessage) {
            if ( nLevel <= LogLevel )
                Debug.Log(log_prefix + sMessage);
        }
        static public void Log(int nLevel, string text, object arg0) {
            if (nLevel <= LogLevel)
                Debug.Log(log_prefix + string.Format(text, arg0));
        }
        static public void Log(int nLevel, string text, object arg0, object arg1) {
            if (nLevel <= LogLevel)
                Debug.Log(log_prefix + string.Format(text, arg0, arg1));
        }
        static public void Log(int nLevel, string text, params object[] args) {
            if (nLevel <= LogLevel)
                Debug.Log(log_prefix + string.Format(text, args));
        }
        static public void Log(string text, params object[] args) {
            Debug.Log(log_prefix + string.Format(text, args));
        }

        // Warning always prints
        static public void Warning(string sMessage) {
            Debug.Log(log_prefix + sMessage);
        }
        static public void Warning(string text, params object[] args) {
            Warning(string.Format(text, args));
        }

        // Error always prints and then throws an exception
        static public void Error(string sMessage) {
            Debug.Log(log_prefix + sMessage);
            throw new SystemException("[DebugUtil.Error] " + sMessage);
        }
        static public void Error(string text, params object[] args) {
            Error(string.Format(text, args));
        }



        static public GameObject EmitDebugSphere(string name, Vector3 position, float diameter, Color color, GameObject parent = null, bool bIsInWorldPos = true) {
            if ( FPlatform.InMainThread() == false ) {
                ThreadMailbox.PostToMainThread(() => { DebugUtil.EmitDebugSphere(name, position, diameter, color, parent, bIsInWorldPos); });
                return null;
            }

			GameObject sphere = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			sphere.SetName(name);
			sphere.transform.position = position;
			sphere.transform.localScale = new Vector3(diameter,diameter,diameter);
			sphere.GetComponent<MeshRenderer> ().material.color = color;

            if (parent != null)
                sphere.transform.SetParent(parent.transform, bIsInWorldPos);

			return sphere;
		}



        static public GameObject EmitDebugAABB(string name, Vector3 center, Vector3f dims, Color color, GameObject inCoords = null) {
            if ( FPlatform.InMainThread() == false ) {
                ThreadMailbox.PostToMainThread(() => { DebugUtil.EmitDebugAABB(name, center, dims, color, inCoords); });
                return null;
            }

			if (inCoords != null) {
				Transform curt = inCoords.transform;
				while (curt != null) {
					center = curt.TransformPoint (center);
					curt = curt.parent;
				}
			}
			GameObject box = GameObject.CreatePrimitive (PrimitiveType.Cube);
			box.SetName(name);
			box.transform.position = center;
            box.transform.localScale = dims;
			box.GetComponent<MeshRenderer> ().material.color = color;
			return box;
		}



		static public GameObject EmitDebugLine(string name, Vector3d start, Vector3d end, double diameter, Colorf color,
                                               GameObject parent = null, bool bIsInWorldPos = true) {
            return EmitDebugLine(name, (Vector3f)start, (Vector3f)end, (float)diameter, color, parent, bIsInWorldPos);
        }
		static public GameObject EmitDebugLine(string name, Vector3f start, Vector3f end, float diameter, Colorf color,
                                               GameObject parent = null, bool bIsInWorldPos = true) {

            if ( FPlatform.InMainThread() == false ) {
                ThreadMailbox.PostToMainThread(() => { DebugUtil.EmitDebugLine(name, start, end, diameter, color, parent, bIsInWorldPos); });
                return null;
            }

			GameObject line = new GameObject ();
			line.SetName(name);
            line.transform.position = (bIsInWorldPos) ? start : Vector3f.Zero;
			line.AddComponent<LineRenderer> ();
			LineRenderer lr = line.GetComponent<LineRenderer> ();
			lr.material = MaterialUtil.CreateParticlesMaterial();
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = diameter;
			lr.SetPosition (0, start);
			lr.SetPosition (1, end);

            if (parent != null) {
                lr.useWorldSpace = bIsInWorldPos;
                line.transform.SetParent(parent.transform, bIsInWorldPos);
            }

			return line;
		}
        static public GameObject EmitDebugLine(string name, Vector3f start, Vector3f end, float diameter, Colorf startColor, Colorf endColor,
                                               GameObject parent = null, bool bIsInWorldPos = true)
        {
            if (FPlatform.InMainThread() == false) {
                ThreadMailbox.PostToMainThread(() => { DebugUtil.EmitDebugLine(name, start, end, diameter, startColor, endColor, parent, bIsInWorldPos); });
                return null;
            }

            GameObject line = new GameObject();
            line.SetName(name);
            line.transform.position =  (bIsInWorldPos) ? start : Vector3f.Zero;
            line.AddComponent<LineRenderer>();
            LineRenderer lr = line.GetComponent<LineRenderer>();
            lr.material = MaterialUtil.CreateParticlesMaterial();
            lr.startColor = startColor;
            lr.endColor = endColor;
            lr.startWidth = lr.endWidth = diameter;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            if (parent != null) {
                lr.useWorldSpace = bIsInWorldPos;
                line.transform.SetParent(parent.transform, bIsInWorldPos);
            }

            return line;
        }


		static public GameObject EmitDebugCurve(string name, Vector3d[] curve, bool bClosed, 
                                                float diameter, Colorf startColor, Colorf endColor, 
                                                GameObject parent = null, bool bIsInWorldPos = true) {
            if (FPlatform.InMainThread() == false) {
                ThreadMailbox.PostToMainThread(() => { DebugUtil.EmitDebugCurve(name, curve, bClosed, diameter, startColor, endColor, parent, bIsInWorldPos); });
                return null;
            }

			GameObject line = new GameObject ();
			line.SetName(name);
			line.AddComponent<LineRenderer> ();
			LineRenderer lr = line.GetComponent<LineRenderer> ();
			lr.material = MaterialUtil.CreateParticlesMaterial();
            lr.startColor = startColor;
            lr.endColor = endColor;
            lr.startWidth = lr.endWidth = diameter;
            Vector3[] verts = new Vector3[curve.Length];
            for (int i = 0; i < curve.Length; ++i)
                verts[i] = (Vector3)curve[i];
            lr.positionCount = curve.Length;
            lr.SetPositions(verts);
            lr.loop = bClosed;
            lr.useWorldSpace = (parent == null && bIsInWorldPos);

            if (parent != null)
                line.transform.SetParent(parent.transform, bIsInWorldPos);

			return line;
		}



        static public GameObject EmitDebugFrame(string name, Frame3f f, float fAxisLength, float diameter = 0.05f, GameObject parent = null) {
            if (FPlatform.InMainThread() == false) {
                ThreadMailbox.PostToMainThread(() => { DebugUtil.EmitDebugFrame(name, f, fAxisLength, diameter); });
                return null;
            }

			GameObject frameObj = new GameObject (name);
			GameObject x = EmitDebugLine (name+"_x", f.Origin, f.Origin + fAxisLength * f.X, diameter, Color.red, frameObj, false);
			GameObject y = EmitDebugLine (name+"_y", f.Origin, f.Origin + fAxisLength * f.Y, diameter, Color.green, frameObj, false);
			GameObject z = EmitDebugLine (name+"_z", f.Origin, f.Origin + fAxisLength * f.Z, diameter, Color.blue, frameObj, false);
            if (parent != null)
                frameObj.transform.SetParent(parent.transform, false);
            return frameObj;
		}



        static public fGameObject EmitDebugBox(string name, Box3d box, Colorf color, GameObject parent = null, bool bIsInWorldPos = true)
        {
            if (FPlatform.InMainThread() == false) {
                ThreadMailbox.PostToMainThread(() => { DebugUtil.EmitDebugBox(name, box, color, parent, bIsInWorldPos); });
                return null;
            }
            TrivialBox3Generator boxgen = new TrivialBox3Generator() { Box = box, NoSharedVertices = true, Clockwise = true };
            boxgen.Generate();
            DMesh3 mesh = boxgen.MakeDMesh();
            fMeshGameObject fMeshGO = GameObjectFactory.CreateMeshGO(name, new fMesh(mesh), false, true);
            fMeshGO.SetMaterial(MaterialUtil.CreateStandardMaterialF(color));
            if (parent != null)
                parent.AddChild(fMeshGO, bIsInWorldPos);
            return fMeshGO;
        }



        static public fGameObject EmitDebugMesh(string name, DMesh3 meshIn, Colorf color, GameObject parent = null, bool bIsInWorldPos = true)
        {
            DMesh3 mesh = new DMesh3(meshIn);
            if (FPlatform.InMainThread() == false) {
                ThreadMailbox.PostToMainThread(() => { DebugUtil.EmitDebugMesh(name, mesh, color, parent, bIsInWorldPos); });
                return null;
            }
            fMeshGameObject fMeshGO = GameObjectFactory.CreateMeshGO(name, new fMesh(mesh), false, true);
            fMeshGO.SetMaterial(MaterialUtil.CreateStandardMaterialF(color));
            if (parent != null)
                parent.AddChild(fMeshGO, bIsInWorldPos);
            return fMeshGO;
        }



        static public GameObject EmitDebugCursorSphere(string name, float diameter, Color color)
        {
            if (FContext.ActiveContext_HACK.MouseCameraController is VRMouseCursorController) {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.SetName(name);
                sphere.transform.position =
                        (FContext.ActiveContext_HACK.MouseCameraController as VRMouseCursorController).CurrentCursorPosWorld;
                sphere.transform.localScale = new Vector3(diameter, diameter, diameter);
                sphere.GetComponent<MeshRenderer>().material =
                    MaterialUtil.CreateTransparentMaterial(color, 0.5f);
                MaterialUtil.DisableShadows(sphere);
                sphere.SetLayer(FPlatform.HUDLayer);
                return sphere;
            } else
                throw new Exception("DebugUtil.EmitDebugCursorSphere: only works for VRMouseCursorController!");
        }




        // for checking things...
        public static void print_compare(string prefix, Quaternion q1, g3.Quaternionf q2)
        {
            double dErr = Math.Abs(q1.x - q2.x) + Math.Abs(q1.y - q2.y) + Math.Abs(q1.z - q2.z) + Math.Abs(q1.w - q2.w);
            DebugUtil.Log(2, "{0} {1} {2} err {3}", prefix, q1.ToString("F8"), q2.ToString(), dErr);
        }

        public static void print_compare(string prefix, Vector3 v1, g3.Vector3f v2)
        {
            double dErr = Math.Abs(v1.x - v2.x) + Math.Abs(v1.y - v2.y) + Math.Abs(v1.z - v2.z);
            DebugUtil.Log(2, "{0} {1} {2} err {3}", prefix, v1.ToString("F8"), v2.ToString(), dErr);
        }

        public static void print_compare(string prefix, Frame3f v1, g3.Frame3f v2)
        {
            double dErr =
                (v1.Origin - v2.Origin).Length+
                ((g3.Quaternionf)v1.Rotation - (v2.Rotation)).Length;
            DebugUtil.Log(2, "{0} {1} {2} err {3}", prefix, v1.ToString("F8"), v2.ToString(), dErr);
        }

        public static void print_compare(string prefix, Matrix4x4 v1, g3.Matrix3f v2)
        {
            double dErr = 0;
            for (int r = 0; r < 3; ++r)
                for (int c = 0; c < 3; ++c)
                    dErr += Math.Abs(v1[r, c] - v2[r, c]);
            DebugUtil.Log(2, "{0} {1} {2} err {3}", prefix, v1.ToString("F3"), v2.ToString("F3"), dErr);
        }




        public static void print_transform_tree(GameObject go)
        {
            string sTree = "";

            GameObject cur = go;
            while ( cur != null ) {
                Vector3 local_pos = cur.transform.localPosition;
                Vector3 local_angles = cur.transform.localEulerAngles;
                Vector3 local_scale = cur.transform.localScale;

                Vector3 pos = cur.transform.position;
                Vector3f angles = cur.transform.eulerAngles;

                string s = string.Format("go: {0}   local scale {1} pos {2} angles {3}     POS {4} ANGLES {5}",
                    cur.name, local_scale, local_pos, local_angles, pos, angles);
                sTree = sTree + s + "\n";


                cur = (cur.transform.parent != null) ? cur.transform.parent.gameObject : null;
            }
            Debug.Log(sTree);            
        }




        public static void WriteDebugMesh(IMesh mesh, string sPath)
        {
            WriteOptions options = WriteOptions.Defaults;
            options.bWriteGroups = true;
            StandardMeshWriter.WriteFile(sPath, new List<WriteMesh>() { new WriteMesh(mesh) }, options);
        }
        public static void WriteDebugMesh(IEnumerable<DMesh3> meshes, string sPath)
        {
            DMesh3 combined = new DMesh3(MeshComponents.FaceGroups);
            MeshEditor editor = new MeshEditor(combined);
            int gid = 1;
            foreach (DMesh3 m in meshes)
                editor.AppendMesh(m, gid++);
            WriteDebugMesh(combined, sPath);
        }





        public static void LogHardcopy(string s)
        {
            using (StreamWriter writer = File.AppendText("c:\\scratch\\__FRAME3_LOG.txt")) {
                writer.WriteLine(s);
                writer.Flush();
            }
        }

    }
}

