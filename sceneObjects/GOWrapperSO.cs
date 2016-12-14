using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    class GOWrapperSO : BaseSO
    {
        GameObject parentGO;

        public GOWrapperSO()
        {
        }

        public void Create(GameObject gameObj)
        {
            this.parentGO = gameObj;

            List<GameObject> children = new List<GameObject>();
            UnityUtil.CollectAllChildren(gameObj, children);
            for (int i = 0; i < children.Count; ++i)
                AppendExistingGO(children[i]);
        }



        //
        // SceneObject impl
        //
        override public GameObject RootGameObject
        {
            get { return parentGO; }
        }

        // GOWrapperSO is for wrapping GOs that were created elsewhere, which
        //  we probably are not going to serialize (change this behavior via subclass and override)
        override public string UUID
        {
            get { return SceneUtil.InvalidUUID; }
        }

        override public string Name
        {
            get { return parentGO.GetName(); }
            set { parentGO.SetName(value); }
        }

        // [RMS] not sure this is the right thing to do...not actually using this type right now??
        override public SOType Type { get { return SOTypes.Unknown; } }


        override public SceneObject Duplicate()
        {
            GOWrapperSO copy = new GOWrapperSO();
            GameObject duplicateGO = UnityEngine.Object.Instantiate(this.parentGO);
            copy.Create(duplicateGO);
            copy.AssignSOMaterial(GetAssignedSOMaterial());
            return copy;
        }


        override public void AssignSOMaterial(SOMaterial m)
        {
            base.AssignSOMaterial(m);
        }
        override public SOMaterial GetAssignedSOMaterial()
        {
            return base.GetAssignedSOMaterial();
        }


    }
}
