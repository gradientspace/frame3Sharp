using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g3;

namespace f3
{
    public interface ILineSetSource
    {
        LineSet Lines { get; }
    }


    /// <summary>
    /// fLineSetGameObject provides a LineSet instance, which is registered
    /// with the LineRenderingManager.
    /// 
    /// Note that currently you cannot replace the internal LineSet with 
    /// a new instance except at construction. This is because the 
    /// LineRenderingManager holds a reference to this object and may
    /// build caches/etc based on it. Use SafeUpdateLines() to modify
    /// the internal LineSet instance.
    /// 
    /// TODO: many improvements
    /// 
    /// </summary>
    public class fLineSetGameObject : fGameObject, ILineSetSource
    {
        LineSet lines = new LineSet();

        public fLineSetGameObject(string name = "line")
            : base(new GameObject(), FGOFlags.EnablePreRender)
        {
            SetName(name);
            LineRenderingManager.AddLineSet(this);
        }

        public fLineSetGameObject(GameObject baseGO, string name = "line")
            : base(baseGO, FGOFlags.EnablePreRender)
        {
            SetName(name);
            LineRenderingManager.AddLineSet(this);
        }

        public fLineSetGameObject(GameObject baseGO, LineSet lines, string name = "line")
            : base(baseGO, FGOFlags.EnablePreRender)
        {
            SetName(name);
            this.lines = lines;
            LineRenderingManager.AddLineSet(this);
        }

        public override void Destroy()
        {
            LineRenderingManager.RemoveLineSet(this);
            base.Destroy();
        }


        public LineSet Lines {
            get { return lines; }
        }


        public override void SetLayer(int layer, bool bSetOnChildren = false)
        {
            int cur_layer = GetLayer();
            LineRenderingManager.ChangeLayer(this, cur_layer, layer);
            base.SetLayer(layer, bSetOnChildren);
        }


        public void SafeUpdateLines(Action<LineSet> updateF)
        {
            updateF(lines);
        }

        public override void PreRender()
        {
        }
    }








}
