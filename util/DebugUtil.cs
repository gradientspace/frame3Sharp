using System;
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

        static public int LogLevel
        {
            get { return __log_level; }
            set { __log_level = value; }
        }


        // Log prints if the nLevel param is <= LogLevel
        static public void Log(int nLevel, string sMessage) {
            if ( nLevel <= LogLevel )
                Debug.Log(sMessage);
        }
        static public void Log(int nLevel, string text, object arg0) {
            if (nLevel <= LogLevel)
                Debug.Log(string.Format(text, arg0));
        }
        static public void Log(int nLevel, string text, object arg0, object arg1) {
            if (nLevel <= LogLevel)
                Debug.Log(string.Format(text, arg0, arg1));
        }
        static public void Log(int nLevel, string text, params object[] args) {
            if (nLevel <= LogLevel)
                Debug.Log(string.Format(text, args));
        }

        // Warning always prints
        static public void Warning(string sMessage) {
            Debug.Log(sMessage);
        }
        static public void Warning(string text, params object[] args) {
            Warning(string.Format(text, args));
        }

        // Error always prints and then throws an exception
        static public void Error(string sMessage) {
            Debug.Log(sMessage);
            throw new SystemException("[DebugUtil.Error] " + sMessage);
        }
        static public void Error(string text, params object[] args) {
            Error(string.Format(text, args));
        }




        static public GameObject EmitDebugSphere(string name, Vector3 position, float diameter, Color color, GameObject inCoords = null) {
			if (inCoords != null) {
				Transform curt = inCoords.transform;
				while (curt != null) {
					position = curt.TransformPoint (position);
					curt = curt.parent;
				}
			}

			GameObject sphere = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			sphere.SetName(name);
			sphere.transform.position = position;
			sphere.transform.localScale = new Vector3(diameter,diameter,diameter);
			sphere.GetComponent<MeshRenderer> ().material.color = color;
			return sphere;
		}



		static public GameObject EmitDebugLine(string name, Vector3 start, Vector3 end, float diameter, Color color) {
			GameObject line = new GameObject ();
			line.SetName(name);
			line.transform.position = start;
			line.AddComponent<LineRenderer> ();
			LineRenderer lr = line.GetComponent<LineRenderer> ();
			lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = diameter;
			lr.SetPosition (0, start);
			lr.SetPosition (1, end);
			return line;
		}
        static public GameObject EmitDebugLine(string name, Vector3 start, Vector3 end, float diameter, Color startColor, Color endColor)
        {
            GameObject line = new GameObject();
            line.SetName(name);
            line.transform.position = start;
            line.AddComponent<LineRenderer>();
            LineRenderer lr = line.GetComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
            lr.startColor = startColor;
            lr.endColor = endColor;
            lr.startWidth = lr.endWidth = diameter;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            return line;
        }


        static public void EmitDebugFrame(string name, Frame3f f, float fAxisLength, float diameter = 0.05f) {
			GameObject frame = new GameObject (name);
			GameObject x = EmitDebugLine (name+"_x", f.Origin, f.Origin + fAxisLength * f.X, diameter, Color.red);
			x.transform.parent = frame.transform;
			GameObject y = EmitDebugLine (name+"_y", f.Origin, f.Origin + fAxisLength * f.Y, diameter, Color.green);
			y.transform.parent = frame.transform;
			GameObject z = EmitDebugLine (name+"_z", f.Origin, f.Origin + fAxisLength * f.Z, diameter, Color.blue);
			z.transform.parent = frame.transform;
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

    }
}

