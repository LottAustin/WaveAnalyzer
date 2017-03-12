using System;
using System.Threading;

namespace WaveAnalyer
{
    class Fourier
    {
        /*
         * Discreate Fourier Transform method
         *
         * Calculates the fourier transform and passes it back as Complex[] A, and then graphs it
         * 
         * Parameter:
         *          s an array of doubles that represent a sine wave
         *          A reference to the container for the DFT
         *          f Form1 (main form)
         *
         * Return void
         */
        public static void DFT(double[] s, double[] s2, ref Complex[] A, ref Complex[] A2, Form1 f) {
            int N = s.Length;
            int N2 = 0;
            Complex[] a = new Complex[N], a2 = null;

            if (s2 != null)
            {
                N2 = s2.Length;
                a2 = new Complex[N2];
            }

            for (int i = 0; i < N; i++)
            {
                a[i] = new Complex();

                if(s2 != null)
                {
                    a2[i] = new Complex();
                }
            }

            Thread dftF = new Thread(() => DFTfront(s, ref a, N));
            Thread dftB = new Thread(() => DFTback(s, ref a, N));
            Thread dftF2 = null;
            Thread dftB2 = null;

            if (s2 != null)
            {
                dftF2 = new Thread(() => DFTfront(s2, ref a2, N));
                dftB2 = new Thread(() => DFTback(s2, ref a2, N));
            }

            dftF.Start();
            dftB.Start();

            if (s2 != null)
            {
                dftF2.Start();
                dftB2.Start();
            }

            dftF.Join();
            dftB.Join();

            if (s2 != null)
            {
                dftF2.Join();
                dftB2.Join();
            }

            A = a;

            if (a2 != null)
            {
                A2 = a2;
            }

            f.Invoke(Form1.dft, new object[] { A, A2 });
            f.Invoke(Form1.btnEnble, new object[] { f.button2,
                    f.button3,
                    f.button4,
                    f.PlayBtn,
                    f.StopBtn,
                    f.WndBtn,
                    f.FilterBtn,
                    f.exportToolStripMenuItem,
                    f.cutToolStripMenuItem,
                    f.copyToolStripMenuItem,
                });
        }

        /*
         * Discreate Fourier Transform front
         *
         * Calculates the fourier transform for the front half of waveform and passes it back in Complex[] A
         * 
         * Parameter:
         *          s an array of doubles that represent a sine wave
         *          A reference to the container for the DFT
         *          N size of the same set
         *
         * Return void
         */
        private static void DFTfront(double[] s, ref Complex[]A, int N)
        {
            for (int f = 0; f < N/2; f++)
            {
                double re = 0;
                double im = 0;

                for (int t = 0; t < N; t++)
                {
                    re += s[t] * Math.Cos(2 * Math.PI * t * f / (double)N);
                    im += -s[t] * Math.Sin(2 * Math.PI * t * f / (double)N);
                }

                A[f].setReal(re);
                A[f].setImaginary(im);
            }
        }

        /*
         * Discreate Fourier Transform back
         *
         * Calculates the fourier transform for the back half of waveform and passes it back in Complex[] A
         * 
         * Parameter:
         *          s an array of doubles that represent a sine wave
         *          A reference to the container for the DFT
         *          N size of the same set
         *
         * Return void
         */
        private static void DFTback(double[] s, ref Complex[] A, int N)
        {
            for (int f = N/2; f < N; f++)
            {
                double re = 0;
                double im = 0;

                for (int t = 0; t < N; t++)
                {
                    re += s[t] * Math.Cos(2 * Math.PI * t * f / (double)N);
                    im += -s[t] * Math.Sin(2 * Math.PI * t * f / (double)N);
                }

                A[f].setReal(re);
                A[f].setImaginary(im);
            }
        }

        /*
         * Inverse Fourier Transform
         *
         *  Caclulates the Inverse Fourier Transform and graphs it
         * 
         * Parameter:
         *          A an array of Complex numbers that holds the DFT
         *          s refernce to the array of samples
         *          form Form1 (main form)
         *
         * Return void
         */
        public static void Inverse(Complex[] A, ref double[] s, Form1 form) {
            int N = A.Length;
            s = new double[N];

            for (int t = 0; t < N; t++)
            {
                for (int f = 0; f < N; f++)
                {
                    s[t] += (((A[f].getReal()*Math.Cos(2*Math.PI*t*f/(double)N)) 
                        - (A[f].getImaginary()*Math.Sin(2*Math.PI*t*f/(double)N))));
                }
                s[t] = s[t] / (double)N;
            }

            form.Invoke(form.inv, new object[] { s });
        }
    }
}
