using System;
using System.Collections.Generic;
using UnityEngine;

namespace f3
{
    public class ScreenCaptureBehavior : StandardInputBehavior
    {
        public string ScreenshotPath = "C:\\";
        public string ScreenshotPrefix = "Screenshot_";

        public ScreenCaptureBehavior()
        {
        }

        public override InputDevice SupportedDevices
        {
            get { return InputDevice.AnySpatialDevice; }
        }

        public override CaptureRequest WantsCapture(InputState input)
        {
            throw new NotImplementedException("ScreenCaptureBehavior.WantsCapture: this is an override behavior and does not capture!!");
        }


        public override Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            throw new NotImplementedException("ScreenCaptureBehavior.BeginCapture: this is an override behavior and does not capture!!");
        }

        public override Capture UpdateCapture(InputState input, CaptureData data)
        {
            if (input.bLeftMenuButtonReleased) {

                string sDate =
                    DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss"); // + DateTime.Now.ToString("tt").ToLower();

                bool bDone = false;
                int i = 0;
                while (!bDone && i < 100) {
                    string sPath = ScreenshotPath + ScreenshotPrefix + sDate +
                        ((i == 0) ? "" : "_" + i.ToString()) + ".png";
                    i++;
                    if (System.IO.File.Exists(sPath))
                        continue;

                    ScreenCapture.CaptureScreenshot(sPath, 4);
                    DebugUtil.Log(0, "Wrote screenshot " + sPath);
                    bDone = true;
                }
            }
            return Capture.Ignore;
        }

        public override Capture ForceEndCapture(InputState input, CaptureData data)
        {
            throw new NotImplementedException("ScreenCaptureBehavior.ForceEndCapture: this is an override behavior and does not capture!!");
        }
    }
}
