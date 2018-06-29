using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V4.App;
using static Android.Views.View;
using Android.Views;
using Android.Graphics;
using Android.Runtime;
using Java.Lang;
using Android.Util;
using Android.Hardware;
using System;
using Android.Content.Res;
using Java.IO;
using System.IO;
using System.Collections.Generic;

namespace AReality
{
    [Activity(Label = "AReality", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : FragmentActivity, IOnTouchListener, ISurfaceHolderCallback,Android.Hardware.Camera.IPictureCallback
    {
        private Matrix matrix = new Matrix();
        private Matrix framMatrix =new Matrix();
        private Matrix savedMatrix = new Matrix();
        private const int NONE = 0;
        private const int DRAG = 1;
        private const int ZOOM = 2;
        private int mode = NONE;
        private PointF start = new PointF();
        private PointF mid = new PointF();
        private float oldDist = 1f;
        private float d = 0f;
        private float newRot = 0f;
        private float[] lastEvent = null;
        string logoImageId = "";
        Bitmap bitmap = null,bitmap2 = null;
        private Android.Hardware.Camera camera = null;
        private SurfaceView cameraSurfaceView = null;
        private ISurfaceHolder cameraSurfaceHolder = null;
        private bool previewing = false;
        RelativeLayout relativeLayout;
        int currentCameraId = 0;
        private Button btnCapture = null;
        ImageButton useOtherCamera = null;
        ImageView logoImageView,frameImageView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.SetFormat(Format.Translucent);
            RequestWindowFeature(WindowFeatures.NoTitle);
            Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);

            SetContentView(Resource.Layout.Main);
            logoImageView = (ImageView)FindViewById(Resource.Id.logoImageView);//
            frameImageView = (ImageView)FindViewById(Resource.Id.frameImageView);
           
            Bundle extras = Intent.Extras;
            if (extras != null)
            {
                logoImageId = extras.GetString("logoImageId ");
            }
            try
            {
                /*File file = new File(Environment.getExternalStorageDirectory()
                        + "/" + getPackageName() + "/logo/" + logoImageId
                        + ".jpg");
                bitmap = BitmapFactory.decodeFile(file.getAbsolutePath());*/
                bitmap = BitmapFactory.DecodeResource(Resources, Resource.Mipmap.trophy);
                bitmap2 = BitmapFactory.DecodeResource(Resources, Resource.Mipmap.frameCamera);
                logoImageView.SetImageBitmap(bitmap);
                frameImageView.SetImageBitmap(bitmap2);
            }
            catch (Java.Lang.Exception e)
            {
                // TODO Auto-generated catch block
                e.PrintStackTrace();
            }
            framMatrix.PostTranslate(0,0);
            framMatrix.SetScale(1, 1.5f);
            frameImageView.ImageMatrix = framMatrix;

            logoImageView.SetOnTouchListener(this);
            relativeLayout = (RelativeLayout)FindViewById(Resource.Id.containerImg);
            relativeLayout.DrawingCacheEnabled = (true);
            cameraSurfaceView = (SurfaceView)FindViewById(Resource.Id.surfaceView);
            cameraSurfaceHolder = cameraSurfaceView.Holder;
            cameraSurfaceHolder.AddCallback(this);
            btnCapture = (Button)FindViewById(Resource.Id.button);
            btnCapture.Click += (sender, e) =>
            {
                // TODO Auto-generated method stub
                camera.TakePicture(null, null, this);
            };

        }

        /**
        * Determine the space between the first two fingers
        */
        private float Spacing(MotionEvent e)
        {
            float x = e.GetX(0) - e.GetX(1);
            float y = e.GetY(0) - e.GetY(1);
            return FloatMath.Sqrt(x * x + y * y);
        }

        /**
         * Calculate the mid point of the first two fingers
         */
        private void MidPoint(PointF point, MotionEvent e)
        {
            float x = e.GetX(0) + e.GetX(1);
            float y = e.GetY(0) + e.GetY(1);
            point.Set(x / 2, y / 2);
        }

        /**
         * Calculate the degree to be rotated by.
         *
         * @param event
         * @return Degrees
         */
            private float Rotation(MotionEvent e) {
            double delta_x = (e.GetX(0) - e.GetX(1));
            double delta_y = (e.GetY(0) - e.GetY(1));
            double radians = Java.Lang.Math.Atan2(delta_y, delta_x);
            return (float) Java.Lang.Math.ToDegrees(radians);
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            ImageView view = (ImageView)v;
            switch (e.Action & MotionEventActions.Mask) {
                case MotionEventActions.Down:
                    savedMatrix.Set(matrix);
                    start.Set(e.GetX(), e.GetY());
                    mode = DRAG;
                    lastEvent = null;
                    break;
                case MotionEventActions.PointerDown:
                    oldDist = Spacing(e);
                    if (oldDist > 10f) {
                        savedMatrix.Set(matrix);
                        MidPoint(mid, e);
                        mode = ZOOM;
                    }
                    lastEvent = new float[4];
                    lastEvent[0] = e.GetX(0);
                    lastEvent[1] = e.GetX(1);
                    lastEvent[2] = e.GetY(0);
                    lastEvent[3] = e.GetY(1);
                    d = Rotation(e);
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.PointerUp:
                    mode = NONE;
                    lastEvent = null;
                    break;
                case MotionEventActions.Move:
                    if (mode == DRAG)
                    {
                        matrix.Set(savedMatrix);
                        float dx = e.GetX() - start.X;
                        float dy = e.GetY() - start.Y;
                        matrix.PostTranslate(dx, dy);
                    } else if (mode == ZOOM) {
                        float newDist = Spacing(e);
                        if (newDist > 10f) {
                            matrix.Set(savedMatrix);
                            float scale = (newDist / oldDist);
                            matrix.PostScale(scale, scale, mid.X, mid.Y);
                        }
                        if (lastEvent != null && e.PointerCount == 3) {
                        newRot = Rotation(e);
                        float r = newRot - d;
                        float[] values = new float[9];
                        matrix.GetValues(values);
                        float tx = values[2];
                        float ty = values[5];
                        float sx = values[0];
                        float xc = (view.Width / 2) * sx;
                        float yc = (view.Height / 2) * sx;
                        matrix.PostRotate(r, tx + xc, ty + yc);
                    }
                }
                break;
            }
            view.ImageMatrix = (matrix);
            return true;
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            // TODO Auto-generated method stub

            if (previewing)
            {
                camera.StopPreview();
                previewing = false;
            }
            try
            {
                float w = 0;
                float h = 0;

                if (this.Resources.Configuration.Orientation != Android.Content.Res.Orientation.Landscape)
                {
                    camera.SetDisplayOrientation(90);
                    Android.Hardware.Camera.Size cameraSize = camera.GetParameters().PictureSize;
                    int wr = relativeLayout.Width;
                    int hr = relativeLayout.Height;
                    float ratio = relativeLayout.Width * 1f / cameraSize.Height;
                    w = cameraSize.Width * ratio;
                    h = cameraSize.Height * ratio;
                    RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)h, (int)w);
                    cameraSurfaceView.LayoutParameters = (lp);
                    frameImageView.LayoutParameters = lp;
                }
                else
                {
                    camera.SetDisplayOrientation(0);
                    Android.Hardware.Camera.Size cameraSize = camera.GetParameters().PictureSize;
                    float ratio = relativeLayout.Height * 1f / cameraSize.Height;
                    w = cameraSize.Width * ratio;
                    h = cameraSize.Height * ratio;
                    RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)w, (int)h);
                    cameraSurfaceView.LayoutParameters = (lp);
                    frameImageView.LayoutParameters = lp;
                }

                camera.SetPreviewDisplay(cameraSurfaceHolder);
                camera.StartPreview();
                previewing = true;
                //float imgHeight = frameImageView.Height;
                //float imgeWidth = frameImageView.Width;

                float imgeWidth = frameImageView.Drawable.IntrinsicWidth;
                float imgHeight = frameImageView.Drawable.IntrinsicHeight;

                DisplayMetrics displayMetrics = new DisplayMetrics();
                WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
                int widthS = displayMetrics.WidthPixels;

                float hR = h / imgHeight;
                float wR = w / widthS;
                //framMatrix.SetScale(2*wR,2*hR);
                frameImageView.SetScaleType(ImageView.ScaleType.FitXy);
            }
            catch (System.Exception e)
            {
                // TODO Auto-generated catch block
                System.Console.WriteLine("SurfaceChanged:"+e.ToString());
            }
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            // TODO Auto-generated method stub
            try
            {
                camera = Android.Hardware.Camera.Open();
                Android.Hardware.Camera.Parameters param = camera.GetParameters();

                // Check what resolutions are supported by your camera
                IList<Android.Hardware.Camera.Size> sizes = param.SupportedPictureSizes;

                // setting small image size in order to avoid OOM error
                Android.Hardware.Camera.Size cameraSize = null;
                foreach(Android.Hardware.Camera.Size size in sizes)
                {
                    //set whatever size you need
                    //if(size.height<500) {
                    cameraSize = size;
                    break;
                    //}
                }

                if (cameraSize != null)
                {
                    param.SetPictureSize(cameraSize.Width, cameraSize.Height);
                    camera.SetParameters(param);

                    float ratio = relativeLayout.Height * 1f / cameraSize.Height;
                    float w = cameraSize.Width * ratio;
                    float h = cameraSize.Height * ratio;
                    RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)w, (int)h);
                    cameraSurfaceView.LayoutParameters = (lp);
                }
            }
            catch (RuntimeException e)
            {
                Toast.MakeText(
                        ApplicationContext,
                        "Device camera  is not working properly, please try after sometime.",
                    ToastLength.Long).Show();
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            // TODO Auto-generated method stub
            camera.StopPreview();
            camera.Release();
            camera = null;
            previewing = false;
        }

        public void OnPictureTaken(byte[] data, Android.Hardware.Camera camera)
        {
            // TODO Auto-generated method stub
            BitmapFactory.Options options = new BitmapFactory.Options();
            //o.inJustDecodeBounds = true;
            Bitmap cameraBitmapNull = BitmapFactory.DecodeByteArray(data, 0, data.Length, options);

            int wid = options.OutWidth;
            int hgt = options.OutHeight;
            Matrix nm = new Matrix();

            Android.Hardware.Camera.Size cameraSize = camera.GetParameters().PictureSize;
            float ratio = relativeLayout.Height * 1f / cameraSize.Height;
            if (Resources.Configuration.Orientation != Android.Content.Res.Orientation.Landscape)
            {
                nm.PostRotate(90);
                nm.PostTranslate(hgt, 0);
                wid = options.OutHeight;
                hgt = options.OutWidth;
                ratio = relativeLayout.Width * 1f / cameraSize.Height;

            }
            else
            {
                wid = options.OutWidth;
                hgt = options.OutHeight;
                ratio = relativeLayout.Height * 1f / cameraSize.Height;
            }

            float[] f = new float[9];
            float[] f1 = new float[9];
            matrix.GetValues(f);
            framMatrix.GetValues(f1);
            f[0] = f[0] / ratio;
            f[4] = f[4] / ratio;
            f[5] = f[5] / ratio;
            f[2] = f[2] / ratio;

            f1[0] = f1[0] / ratio;
            f1[4] = f1[4] / ratio;
            f1[5] = f1[5] / ratio;
            f1[2] = f1[2] / ratio;
            matrix.SetValues(f);
            framMatrix.SetValues(f1);
            Bitmap newBitmap = Bitmap.CreateBitmap(wid, hgt,Bitmap.Config.Argb8888);

            Canvas canvas = new Canvas(newBitmap);
            Bitmap cameraBitmap = BitmapFactory.DecodeByteArray(data, 0,data.Length, options);

            canvas.DrawBitmap(cameraBitmap, nm, null);
            cameraBitmap.Recycle();

            canvas.DrawBitmap(bitmap, matrix, null);
            bitmap.Recycle();


            Bitmap bbb = Bitmap.CreateScaledBitmap(bitmap2, cameraBitmap.Height, cameraBitmap.Width, false);
            canvas.DrawBitmap(bbb, new Matrix(), null);
            bbb.Recycle();

            Java.IO.File storagePath = new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory + "/PhotoAR/");
            storagePath.Mkdirs();

            Java.IO.File myImage = new Java.IO.File(storagePath, Long.ToString(JavaSystem.CurrentTimeMillis()) + ".jpg");

            try
            {
                //string path = System.IO.Path.Combine(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).AbsolutePath, "newProdict.png");
                string path = myImage.AbsolutePath;
                var fs = new FileStream(path, FileMode.OpenOrCreate);
                if (fs != null)
                {
                    newBitmap.Compress(Bitmap.CompressFormat.Jpeg, 80, fs);
                    fs.Close();
                }
                //Stream outs = new FileOutputStream(myImage);
                //newBitmap.Compress(Bitmap.CompressFormat.Jpeg, 80, outs);

                //out.flush();
                //out.close();
            }
            catch (System.Exception e)
            {
                Log.Debug("In Saving File", e + "");
            }
        }
    }

}

