using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using g3;

namespace f3 {

    // extension to geometry3Sharp.MeshGenerator so we can convert to UnityEngine.Mesh
    static public class MeshGenExt
    {
        public static Vector3[] ToVector3(VectorArray3f a)
        {
            Vector3[] v = new Vector3[a.Count];
            for (int i = 0; i < a.Count; ++i) {
                v[i].x = a.array[3 * i];
                v[i].y = a.array[3 * i + 1];
                v[i].z = a.array[3 * i + 2];
            }
            return v;
        }
        public static Vector3[] ToVector3(VectorArray3d a)
        {
            Vector3[] v = new Vector3[a.Count];
            for (int i = 0; i < a.Count; ++i) {
                v[i].x = (float)a.array[3 * i];
                v[i].y = (float)a.array[3 * i + 1];
                v[i].z = (float)a.array[3 * i + 2];
            }
            return v;
        }
        public static Vector2[] ToVector2(VectorArray2f a)
        {
            Vector2[] v = new Vector2[a.Count];
            for (int i = 0; i < a.Count; ++i) {
                v[i].x = (float)a.array[2 * i];
                v[i].y = (float)a.array[2 * i + 1];
            }
            return v;
        }

        public static Mesh MakeUnityMesh(this MeshGenerator gen, bool bRecalcNormals = false)
        {
            Mesh m = new Mesh();
            m.vertices = ToVector3(gen.vertices);
            if ( gen.uv != null && gen.WantUVs )
                m.uv = ToVector2(gen.uv);
            if (gen.normals != null && gen.WantNormals)
                m.normals = ToVector3(gen.normals);
            m.triangles = gen.triangles.array;

            if ( bRecalcNormals )
                m.RecalculateNormals();

            return m;
        }
    }




	public class MeshGenerators
    {


        public static Mesh MakeMesh(Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles)
        {
            Mesh m = new Mesh();
            m.vertices = vertices;
            m.uv = uv;
            m.normals = normals;
            m.triangles = triangles;
            return m;
        }


        // create a triangle fan
        public static Mesh CreateTrivialDisc(float radius, int nSteps, float fStartAngleDeg = 0.0f, float fEndAngleDeg = 360.0f) {
            TrivialDiscGenerator gen = new TrivialDiscGenerator() 
                { Slices = nSteps, Clockwise = true, Radius = radius, StartAngleDeg = fStartAngleDeg, EndAngleDeg = fEndAngleDeg };
            gen.Generate();
            return gen.MakeUnityMesh();
        }

        public static Mesh CreatePuncturedDisc(float innerRadius, float outerRadius, int nSteps, float fStartAngleDeg = 0.0f, float fEndAngleDeg = 360.0f)
        {
            PuncturedDiscGenerator gen = new PuncturedDiscGenerator() 
            { Slices = nSteps, Clockwise = true, InnerRadius = innerRadius, OuterRadius = outerRadius,
                StartAngleDeg = fStartAngleDeg, EndAngleDeg = fEndAngleDeg };
            gen.Generate();
            return gen.MakeUnityMesh();
        }


        public static Mesh CreateCylinder(float radius, float height, int nSlices, float fStartAngleDeg = 0.0f, float fEndAngleDeg = 360.0f)
        {
            CappedCylinderGenerator gen = new CappedCylinderGenerator() {
                BaseRadius = radius, TopRadius = radius, Height = height, Slices = nSlices,
                Clockwise = true, StartAngleDeg = fStartAngleDeg, EndAngleDeg = fEndAngleDeg
            };
            gen.Generate();
            return gen.MakeUnityMesh(false);
        }



        public static Mesh Create3DArrow(float Length, int nSlices)
        {
            Radial3DArrowGenerator gen = new Radial3DArrowGenerator() {
                Clockwise = true, Slices = nSlices
            };
            gen.HeadLength = Length / 3.0f;
            gen.HeadBaseRadius = gen.HeadLength * 0.5f;
            gen.StickLength = Length - gen.HeadLength;
            gen.StickRadius = 0.5f * gen.HeadBaseRadius;
            gen.Generate();
            return gen.MakeUnityMesh(true);
        }
        public static Mesh Create3DArrow(float Length, float Width, int nSlices)
        {
            Radial3DArrowGenerator gen = new Radial3DArrowGenerator() {
                Clockwise = true, Slices = nSlices
            };
            gen.HeadLength = Length / 3.0f;
            gen.HeadBaseRadius = Width * 0.5f;
            gen.StickLength = Length - gen.HeadLength;
            gen.StickRadius = 0.5f * gen.HeadBaseRadius;
            gen.Generate();
            return gen.MakeUnityMesh(true);
        }
        public static Mesh Create3DArrow(float HeadLength, float HeadWidth, float StickLength, float StickWidth, int nSlices)
        {
            Radial3DArrowGenerator gen = new Radial3DArrowGenerator() {
                Clockwise = true, Slices = nSlices, 
                HeadLength = HeadLength, HeadBaseRadius = HeadWidth*0.5f,
                StickLength = StickLength, StickRadius = StickWidth*0.5f
            };
            gen.Generate();
            return gen.MakeUnityMesh(true);
        }



        // two-triangle rectangle
        public enum UVRegionType
        {
            FullUVSquare,
            CenteredUVRectangle,
            BottomCornerUVRectangle
        }
        public static Mesh CreateTrivialRect(float width, float height, UVRegionType eUVType)
        {
            TrivialRectGenerator gen = new TrivialRectGenerator() 
                { Width = width, Height = height, Clockwise = true };
            if (eUVType == UVRegionType.CenteredUVRectangle)
                gen.UVMode = TrivialRectGenerator.UVModes.CenteredUVRectangle;
            else if (eUVType == UVRegionType.BottomCornerUVRectangle)
                gen.UVMode = TrivialRectGenerator.UVModes.BottomCornerUVRectangle;
            gen.Generate();
            return gen.MakeUnityMesh();
        }




        // from http://answers.unity3d.com/questions/855827/problems-with-creating-a-disk-shaped-mesh-c.html
        // ugh this code is shit! 
        public static Mesh CreateDisc(float radius, int radiusTiles,int tilesAround)
		{
			Vector3[] vertices = new Vector3    [radiusTiles*tilesAround*6];
			Vector3[] normals = new Vector3[vertices.Length];
			int[] triangles     = new int        [radiusTiles*tilesAround*6];
			Vector2[] UV = new Vector2[vertices.Length];
			int currentVertex = 0;

			float tileLength = radius / (float)radiusTiles;    //the length of a tile parallel to the radius
			float radPerTile = 2 * Mathf.PI / tilesAround; //the radians the tile takes

			for(int angleNum = 0; angleNum < tilesAround; angleNum++)//loop around
			{
				float angle = (float)radPerTile*(float)angleNum;    //the current angle in radians

				for(int offset = 0; offset < radiusTiles; offset++)//loop from the center outwards
				{
					vertices[currentVertex]        =    new Vector3(Mathf.Cos(angle)*offset*tileLength                 ,0,  Mathf.Sin(angle)*offset*tileLength);
					vertices[currentVertex + 1]    =    new Vector3(Mathf.Cos(angle + radPerTile)*offset*tileLength ,0,  Mathf.Sin(angle + radPerTile)*offset*tileLength);
					vertices[currentVertex + 2]    =    new Vector3(Mathf.Cos(angle)*(offset + 1)*tileLength         ,0,  Mathf.Sin(angle)*(offset + 1)*tileLength);					

					vertices[currentVertex + 3]    =    new Vector3(Mathf.Cos(angle + radPerTile)*offset*tileLength         ,0,  Mathf.Sin(angle + radPerTile)*offset*tileLength);
					vertices[currentVertex + 4]    =    new Vector3(Mathf.Cos(angle + radPerTile)*(offset + 1)*tileLength     ,0,  Mathf.Sin(angle + radPerTile)*(offset + 1)*tileLength);
					vertices[currentVertex + 5]    =    new Vector3(Mathf.Cos(angle)*(offset + 1)*tileLength                 ,0,  Mathf.Sin(angle)*(offset + 1)*tileLength);

					currentVertex += 6;
				}
			}

			for(int j = 0; j < triangles.Length; j++)    //set the triangles
			{
				triangles[j] = j;
			}

			for (int k = 0; k < vertices.Length; ++k)
				normals [k] = Vector3.up;

			//create the mesh and apply vertices/triangles/UV
			Mesh disk = new Mesh();
			disk.vertices = vertices;
			disk.triangles = triangles;
			disk.normals = normals;
			disk.uv = UV;    //the UV doesnt need to be set

			return disk;
		}


		// from http://wiki.unity3d.com/index.php/ProceduralPrimitives#C.23_-_Tube
		public static Mesh CreateCylider(float radius, float height = 1, int nbSides = 24)
		{
			// Outter shell is at radius1 + radius2 / 2, inner shell at radius1 - radius2 / 2
			float bottomRadius = radius;
			float topRadius = radius;

			// vertices & uvs

			Vector3[] vertices = new Vector3[nbSides * 2 + 2];
			Vector3[] normals = new Vector3[vertices.Length];
			Vector2[] uvs = new Vector2[vertices.Length];
			float _2pi = Mathf.PI * 2f;

			// Sides (out)
			int vert = 0;
			for ( int vi = 0; vi <= nbSides; vi++ ) {
				int k = (vi == nbSides) ? 0 : vi;
				float t = (float)(vi) / nbSides;

				float r1 = (float)(k) / nbSides * _2pi;
				float cos = Mathf.Cos(r1);
				float sin = Mathf.Sin(r1);

				vertices[vert] = new Vector3(cos * topRadius, height, sin * topRadius);
				vertices[vert + 1] = new Vector3(cos * bottomRadius, 0, sin * bottomRadius);
				uvs [vert] = new Vector2 (t, 0f);
				uvs [vert + 1] = new Vector2 (t, 1f);
				vert+=2;
			}

			for (int vi = 0; vi < vertices.Length; ++vi)
				normals [vi] = new Vector3 (vertices [vi].x, 0, vertices [vi].z).normalized;

			//Triangles
			int nbFace = nbSides;
			int nbTriangles = nbFace * 2;
			int nbIndexes = nbTriangles * 3;
			int[] triangles = new int[nbIndexes];

			// Sides (out)
			int ti = 0;
			for ( int vi = 0; vi < nbSides; vi++ ) {
				int current = 2*vi;
				int next = 2*vi+2;

				triangles[ ti++ ] = current;
				triangles[ ti++ ] = next;
				triangles[ ti++ ] = next + 1;

				triangles[ ti++ ] = current;
				triangles[ ti++ ] = next + 1;
				triangles[ ti++ ] = current + 1;
			}



			Mesh cylinder = new Mesh();
			cylinder.vertices = vertices;
			cylinder.normals = normals;
			cylinder.uv = uvs;
			cylinder.triangles = triangles;

			cylinder.RecalculateBounds();
			cylinder.Optimize();

			return cylinder;
		}


	}

} // end namespace 