﻿using System;
using FaceRecognitionDotNet;
using osu.Framework.IO.Stores;
using HoloTrack.Resources;
using System.IO;
using System.Drawing;
using System.Linq;

namespace HoloTrack.Vision
{
    /// <summary>
    /// A class that implements face targeting using DLib.
    /// </summary>
    public class Face
    {
        private static readonly FaceRecognition faceRecognition;
        private static readonly Camera camera;

        /// <summary>
        /// Loads the Appropriate DLib Model for inference.
        /// </summary>
        /// <param name="model">The name of the model, note this must exist inside HoloTrack.Resources.</param>
        /// <returns>The byte array for the model.</returns>
        internal static byte[] LoadModel(string model)
        {
            var storage = new DllResourceStore(typeof(HoloTrackResource).Assembly);

            return storage.Get($"Models/{model}.dat");
        }

        /// <summary>
        /// Perform Inference and get all valid targets. Note that you must execute this asynchronously otherwise this will block the main thread.
        /// </summary>
        /// <param name="model">the name of the model as defined in HoloTrack.Resources.</param>
        public static Location[] GetTargets()
        {
            var cameraStream = Camera.CreateCameraVideoByte();

            using (var ms = new MemoryStream(cameraStream))
            {
                // load our camera stream into a Bitmap, then load it for inference.
                var imageFromByte = (Bitmap)System.Drawing.Image.FromStream(ms);
                var image = FaceRecognition.LoadImage(imageFromByte);

                // now we have the stream loaded from the camera, now let's return the amount of faces we detected!
                return faceRecognition.FaceLocations(image).ToArray();
            }
        }

        /// <summary>
        /// Gets all the Landmark data from a target.
        /// </summary>
        /// <param name="faceTarget">the target face location.</param>
        public static System.Collections.Generic.IDictionary<FacePart, System.Collections.Generic.IEnumerable<FacePoint>>[] GetLandmarks(Location faceTarget)
        {
            // We'll need to get the index of our matching target. We'll use this later.
            var faceTargets = GetTargets();
            int target = Array.BinarySearch(faceTargets, faceTarget);

            // A little sanity check so we don't encounter nasty stuff on the long run.
            if (faceTargets[target] !=  faceTarget)
            {
                throw new ArgumentOutOfRangeException("Error: FaceTarget value is not the same as target!");
            }

            // FIXME: use only the faceTarget provided to reduce noise!
            // we can only invocate new data while we're on a loop, so we're going to run a infinite loop so we always get new data.
            while (true)
            {
                byte[] cameraStream = Camera.CreateCameraVideoByte();
                
                using (var ms = new MemoryStream(cameraStream))
                {
                    var image = (Bitmap)System.Drawing.Image.FromStream(ms);

                    using (var faceLandmarks = FaceRecognition.LoadImage(image))
                    {
                        return faceRecognition.FaceLandmark(faceLandmarks).ToArray();
                    }
                }
            }
        }
    }
}