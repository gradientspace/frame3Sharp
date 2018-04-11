/*
 * [RMS] this is a modified version of Unity's built-in CanvasScaler UI component.
 * The main change is to add a configurable scaling factor applied to the DPI.
 * This allows a UI sized in "physical units" to be globally scaled.
 * 
 * Original license is at end of file
 * 
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace f3 {

    [RequireComponent(typeof(Canvas))]
    [ExecuteInEditMode]
    [AddComponentMenu("f3/F3 Canvas Scaler", 101)]
    public class f3UnityCanvasScaler : UIBehaviour
    {
        [Tooltip("If a sprite has this 'Pixels Per Unit' setting, then one pixel in the sprite will cover one unit in the UI.")]
        [SerializeField] protected float m_ReferencePixelsPerUnit = 100;
        public float referencePixelsPerUnit { get { return m_ReferencePixelsPerUnit; } set { m_ReferencePixelsPerUnit = value; } }


        // Constant Physical Size settings

        public enum Unit { Centimeters, Millimeters, Inches, Points, Picas }

        [Tooltip("The physical unit to specify positions and sizes in.")]
        [SerializeField] protected Unit m_PhysicalUnit = Unit.Points;
        public Unit physicalUnit { get { return m_PhysicalUnit; } set { m_PhysicalUnit = value; } }

        // [RMS] added this
        [Tooltip("physical unit fudge factor")]
        [SerializeField] protected float m_PhysicalUnitFudge = 1.0f;
        public float physicalUnitFudge { get { return m_PhysicalUnitFudge; } set { m_PhysicalUnitFudge = value; } }
        // (end addition)

        [Tooltip("The DPI to assume if the screen DPI is not known.")]
        [SerializeField] protected float m_FallbackScreenDPI = 96;
        public float fallbackScreenDPI { get { return m_FallbackScreenDPI; } set { m_FallbackScreenDPI = value; } }

        [Tooltip("The pixels per inch to use for sprites that have a 'Pixels Per Unit' setting that matches the 'Reference Pixels Per Unit' setting.")]
        [SerializeField] protected float m_DefaultSpriteDPI = 96;
        public float defaultSpriteDPI { get { return m_DefaultSpriteDPI; } set { m_DefaultSpriteDPI = Mathf.Max(1, value); } }


        // General variables

        private Canvas m_Canvas;
        [System.NonSerialized]
        private float m_PrevScaleFactor = 1;
        [System.NonSerialized]
        private float m_PrevReferencePixelsPerUnit = 100;


        protected f3UnityCanvasScaler() { }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Canvas = GetComponent<Canvas>();
            UpdateScaling();
        }

        protected override void OnDisable()
        {
            SetScaleFactor(1);
            SetReferencePixelsPerUnit(100);
            base.OnDisable();
        }

        protected virtual void Update()
        {
            UpdateScaling();
        }

        protected virtual void UpdateScaling()
        {
            if (m_Canvas == null || !m_Canvas.isRootCanvas)
                return;

            float currentDpi = Screen.dpi;
            float dpi = (currentDpi == 0 ? m_FallbackScreenDPI : currentDpi);
            float targetDPI = 1;
            switch (m_PhysicalUnit) {
                case Unit.Centimeters: targetDPI = 2.54f; break;
                case Unit.Millimeters: targetDPI = 25.4f; break;
                case Unit.Inches: targetDPI = 1; break;
                case Unit.Points: targetDPI = 72; break;
                case Unit.Picas: targetDPI = 6; break;
            }
            // [RMS] added this
            targetDPI /= m_PhysicalUnitFudge;
            // (end addition)

            SetScaleFactor(dpi / targetDPI);
            SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit * targetDPI / m_DefaultSpriteDPI);
        }


        protected void SetScaleFactor(float scaleFactor)
        {
            if (scaleFactor == m_PrevScaleFactor)
                return;

            m_Canvas.scaleFactor = scaleFactor;
            m_PrevScaleFactor = scaleFactor;
        }

        protected void SetReferencePixelsPerUnit(float referencePixelsPerUnit)
        {
            if (referencePixelsPerUnit == m_PrevReferencePixelsPerUnit)
                return;

            m_Canvas.referencePixelsPerUnit = referencePixelsPerUnit;
            m_PrevReferencePixelsPerUnit = referencePixelsPerUnit;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_DefaultSpriteDPI = Mathf.Max(1, m_DefaultSpriteDPI);
        }

#endif
    }
}









/*
 * License for this file, downloaded from Unity Technologies bitbucket repository on April 11, 2018
 * https://bitbucket.org/Unity-Technologies/ui
 * 

Unity Companion License(“License”)
Software Copyright © 2014 - 2018 Unity Technologies ApS

Unity Technologies ApS(“Unity”) grants to you a worldwide, non-exclusive, no-charge, and royalty-free copyright license to reproduce, prepare derivative works of, publicly display, publicly perform, sublicense, and distribute the software that is made available under this License (“Software”), subject to the following terms and conditions: 
 
1. Unity Companion Use Only. Exercise of the license granted herein is limited to exercise for the creation, use, and/or distribution of applications, software, or other content pursuant to a valid Unity content authoring and rendering engine software license (“Engine License”). That means while use of the Software is not limited to use in the software licensed under the Engine License, the Software may not be used for any purpose other than the creation, use, and/or distribution of Engine License-dependent applications, software, or other content. No other exercise of the license granted herein is permitted, and in no event may the Software be used for competitive analysis or to develop a competing product or service.
 
2. No Modification of Engine License. Neither this License nor any exercise of the license granted herein modifies the Engine License in any way.
 
3. Ownership & Grant Back to You. 

3.1 You own your content. In this License, “derivative works” means derivatives of the Software itself--works derived only from the Software by you under this License (for example, modifying the code of the Software itself to improve its efficacy); “derivative works” of the Software do not include, for example, games, apps, or content that you create using the Software.You keep all right, title, and interest to your own content.

3.2 Unity owns its content.While you keep all right, title, and interest to your own content per the above, as between Unity and you, Unity will own all right, title, and interest to all intellectual property rights (including patent, trademark, and copyright) in the Software and derivative works of the Software, and you hereby assign and agree to assign all such rights in those derivative works to Unity.

3.3 You have a license to those derivative works. Subject to this License, Unity grants to you the same worldwide, non-exclusive, no-charge, and royalty-free copyright license to derivative works of the Software you create as is granted to you for the Software under this License.
 
4. Trademarks.You are not granted any right or license under this License to use any trademarks, service marks, trade names, products names, or branding of Unity or its affiliates (“Trademarks”). Descriptive uses of Trademarks are permitted; see, for example, Unity’s Branding Usage Guidelines at https://unity3d.com/public-relations/brand.
 
5. Notices & Third-Party Rights.This License, including the copyright notice associated with the Software, must be provided in all substantial portions of the Software and derivative works thereof (or, if that is impracticable, in any other location where such notices are customarily placed). Further, if the Software is accompanied by a Unity “third-party notices” or similar file, you acknowledge and agree that software identified in that file is governed by those separate license terms.
 
6. DISCLAIMER, LIMITATION OF LIABILITY.THE SOFTWARE AND ANY DERIVATIVE WORKS THEREOF IS PROVIDED ON AN "AS IS" BASIS, AND IS PROVIDED WITHOUT WARRANTY OF ANY KIND, WHETHER EXPRESS OR IMPLIED, INCLUDING ANY WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, AND/OR NONINFRINGEMENT. IN NO EVENT SHALL ANY COPYRIGHT HOLDER OR AUTHOR BE LIABLE FOR ANY CLAIM, DAMAGES (WHETHER DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL, INCLUDING PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, LOSS OF USE, DATA, OR PROFITS, AND BUSINESS INTERRUPTION), OR OTHER LIABILITY WHATSOEVER, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM OR OUT OF, OR IN CONNECTION WITH, THE SOFTWARE OR ANY DERIVATIVE WORKS THEREOF OR THE USE OF OR OTHER DEALINGS IN SAME, EVEN WHERE ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
7. USE IS ACCEPTANCE and License Versions. Your receipt and use of the Software constitutes your acceptance of this License and its terms and conditions. Software released by Unity under this License may be modified or updated and the License with it; upon any such modification or update, you will comply with the terms of the updated License for any use of any of the Software under the updated License.
 
8. Use in Compliance with Law and Termination.Your exercise of the license granted herein will at all times be in compliance with applicable law and will not infringe any proprietary rights(including intellectual property rights); this License will terminate immediately on any breach by you of this License.

9. Severability.If any provision of this License is held to be unenforceable or invalid, that provision will be enforced to the maximum extent possible and the other provisions will remain in full force and effect.

10. Governing Law and Venue. This License is governed by and construed in accordance with the laws of Denmark, except for its conflict of laws rules; the United Nations Convention on Contracts for the International Sale of Goods will not apply. If you reside (or your principal place of business is) within the United States, you and Unity agree to submit to the personal and exclusive jurisdiction of and venue in the state and federal courts located in San Francisco County, California concerning any dispute arising out of this License (“Dispute”). If you reside (or your principal place of business is) outside the United States, you and Unity agree to submit to the personal and exclusive jurisdiction of and venue in the courts located in Copenhagen, Denmark concerning any Dispute. 

    */
