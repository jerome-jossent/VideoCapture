using OpenCvSharp;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCapture
{
    internal class Filtre_Ruller : Filtre
    {
        Point2f? A = null;
        Point2f? B = null;
        public float distance;
        Size? matSize = null;

        public Filtre_TXT mesure;

        static Scalar couleur_jaune = new Scalar(0, 255, 255,255);
        static Scalar couleur_magenta = new Scalar(255, 0, 255, 255);
        static Scalar couleur_rouge = new Scalar(255, 0, 0, 255);

        Scalar couleur_point_valide = couleur_rouge;
        Scalar couleur_point_temporaire = couleur_magenta;
        Scalar couleur_ruller = couleur_jaune;

        public bool defined = false;

        public Filtre_Ruller()
        {
            _type = FiltreType.ruller;
            XY = new System.Windows.Point(0.5, 0.5);
            Dynamic = true;
        }

        public override void UpdateTitle()
        {
            if (A == null)
            {
                title1 = PointToString(XY) + " ; ";
                title3 = "B?";
            }
            else if (A != null && B == null)
            {
                title1 = "A" + PointToString(A) + " ; ";
                title3 = PointToString(XY);
                float d = Distance(A, XY);
                title = d.ToString("f3") + " ";
            }
            else if (A != null && B != null)
            {
                title1 = "A" + PointToString(A) + " ; ";
                title3 = "B" + PointToString(B);

                distance = Distance(A, B);
                title = distance.ToString("f3") + " ";
                defined = true;
            }
        }

        System.Windows.Point EntreAetB()
        {
            Point2f a = (Point2f)A;
            Point2f b = (Point2f)B;
            return new System.Windows.Point((a.X + b.X) / 2, (a.Y + b.Y) / 2); 
        }

        string PointToString(System.Windows.Point point)
        {
            return PointToString(WindowsToOpenCV_Point2f(point));
        }

        string PointToString(Point2f? point)
        {
            if (point == null)
                return "null";
            return "(" + point?.X.ToString("f3") + ", " + point?.Y.ToString("f3") + ")";
        }
        float Distance(Point2f? a, System.Windows.Point xy)
        {
            Point2f XY = WindowsToOpenCV_Point2f(xy);
            return Distance(a, XY);
        }

        float Distance(Point2f? a, Point2f? b)
        {
            Point2f A = (Point2f)a;
            Point2f B = (Point2f)b;

            float x = A.X - B.X;
            float y = A.Y - B.Y;

            float hypothenuse = (float)Math.Sqrt(x * x + y * y);
            return hypothenuse;
        }


        Point2f WindowsToOpenCV_Point2f(System.Windows.Point xy, Size size)
        {
            return new Point2f((float)(xy.X * size.Width), (float)(xy.Y * size.Height));
        }

        Point2f WindowsToOpenCV_Point2f(System.Windows.Point xy)
        {
            return new Point2f((float)xy.X, (float)xy.Y);
        }


        internal void MouseMove(Mat filterframe_dynamic)
        {
            UpdateTitle();
           // DrawPointsAndLine(filterframe_dynamic);
        }

        internal Filtre MouseDown(Mat filterframe, Mat filterframedynamic)
        {
            if (A == null)
            {//premier click
                A = WindowsToOpenCV_Point2f(XY);
                //DrawPointsAndLine(filterframe);//, (Point2f)A);
                //UpdateTitle();
                return this;
            }

            //second click
            B = WindowsToOpenCV_Point2f(XY);
            DrawPointsAndLine(filterframe);//, (Point2f)A, (Point2f)B);
            UpdateTitle();
            Dynamic = false;
            return null;
        }

        internal void DrawPointsAndLine(Mat filterframe)
        {
            try
            {
                if (filterframe == null)
                    return;

                if (matSize == null)
                    matSize = filterframe.Size();

                if (A == null)
                {
                    //Draw A temporaire
                    Point2f A_tmp = WindowsToOpenCV_Point2f(XY);
                    DrawPointsAndLine(filterframe, A_tmp, couleur_point_temporaire);
                }
                if (A != null && B == null)
                {
                    //Draw A définitif & B temporaire
                    Point2f B_tmp = WindowsToOpenCV_Point2f(XY);
                    DrawPointsAndLine(filterframe, (Point2f)A, B_tmp, couleur_point_temporaire);
                }
                if (A != null && B != null)
                {
                    //Draw A & B définitifs
                    DrawPointsAndLine(filterframe, (Point2f)A, (Point2f)B, couleur_point_valide);
                }
            }
            catch (Exception ex)
            {
            }
        }

        void DrawPointsAndLine(Mat mat, Point2f a, Scalar couleurA)
        {
            Point2f A = new Point2f((float)(a.X * matSize?.Width), (float)(a.Y * matSize?.Height));

            int delta = 5;
            //Point A
            Point2d x_moins = new Point2d(A.X - delta, A.Y);
            Point2d x_plus = new Point2d(A.X + delta, A.Y);
            Cv2.Line(mat, (Point)x_moins, (Point)x_plus, couleurA, thickness: 1);
            Point2d y_moins = new Point2d(A.X, A.Y - delta);
            Point2d y_plus = new Point2d(A.X, A.Y + delta);
            Cv2.Line(mat, (Point)y_moins, (Point)y_plus, couleurA, thickness: 1);
        }

        void DrawPointsAndLine(Mat mat, Point2f a, Point2f b, Scalar couleurB)
        {
            int delta = 5;

            Point2f A = new Point2f((float)(a.X * matSize?.Width), (float)(a.Y* matSize?.Height));
            Point2f B = new Point2f((float)(b.X * matSize?.Width), (float)(b.Y* matSize?.Height));

            //Ligne entre A et B
            Cv2.Line(mat, (Point)A, (Point)B, couleur_ruller, thickness: 1);

            //Point A
            Point2d x1_moins = new Point2d(A.X - delta, A.Y);
            Point2d x1_plus = new Point2d(A.X + delta, A.Y);
            Cv2.Line(mat, (Point)x1_moins, (Point)x1_plus, couleur_point_valide, thickness: 1);
            Point2d y1_moins = new Point2d(A.X, A.Y - delta);
            Point2d y1_plus = new Point2d(A.X, A.Y + delta);
            Cv2.Line(mat, (Point)y1_moins, (Point)y1_plus, couleur_point_valide, thickness: 1);

            //Point B
            Point2d x2_moins = new Point2d(B.X - delta, B.Y);
            Point2d x2_plus = new Point2d(B.X + delta, B.Y);
            Cv2.Line(mat, (Point)x2_moins, (Point)x2_plus, couleurB, thickness: 1);
            Point2d y2_moins = new Point2d(B.X, B.Y - delta);
            Point2d y2_plus = new Point2d(B.X, B.Y + delta);
            Cv2.Line(mat, (Point)y2_moins, (Point)y2_plus, couleurB, thickness: 1);
        }

        internal Filtre_TXT DisplayMesure()
        {
            mesure = new Filtre_TXT();
            mesure.filtre_TXT_Type = Filtre_TXT.Filtre_TXT_Type.Free;
            mesure.XY = EntreAetB();
            return mesure;
        }
    }
}