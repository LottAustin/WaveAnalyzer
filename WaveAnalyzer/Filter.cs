namespace WaveAnalyer
{
    public class Filter
    {
        /*
        * HPass
        *
        *  High Pass filter function, takes references to the DFT(A) and samples and filters them based on user selection
        * 
        * Parameter:
        *          A        reference to an array of Complex numbers that holds the DFT
        *          s        refernce to the array of samples
        *          bucket   bucket selected on the DFT graph
        *          form     Form1 (main form)
        *
        * Return bool returns false if Nyquist limit or A[0] is selected
        */
        public static bool HPass(ref Complex[] A, ref Complex[] A2, ref double[] samples, ref double[] samples2, int bucket, Form1 form)
        {
            int Nyquist = A.Length / 2, Nyquist2 = 0;
            int start, end;
            double[] filter = new double[0];
            double[] filter2 = new double[0];
            A = new Complex[A.Length];

            if(samples2 != null)
            {
                Nyquist2 = A2.Length / 2;
                A2 = new Complex[A2.Length];
            }

            for (int i = 0; i < A.Length; i++)
            {
                A[i] = new Complex(1,-1);

                if(samples2 != null)
                {
                    A2[i] = new Complex(1, -1);
                }
            }

            if (bucket == 0)
            {
                return false;
            }

            if (bucket < Nyquist)
            {
                start = bucket;
                end = A.Length - bucket;
            }
            else if (bucket > Nyquist)
            {
                start = bucket - Nyquist;
                end = bucket;
            }
            else
            {
                A[Nyquist].setReal(0);
                A[Nyquist].setImaginary(0);

                Fourier.Inverse(A, ref filter, form);

                Convolution(ref samples, filter);

                if (samples2 != null)
                {
                    A2[Nyquist].setReal(0);
                    A2[Nyquist].setImaginary(0);

                    Fourier.Inverse(A2, ref filter2, form);

                    Convolution(ref samples2, filter2);
                }

                return true;
            }

            for (int i = 1; i < start; i++)
            {
                A[i].setReal(0);
                A[i].setImaginary(0);

                if(samples2 != null)
                {
                    A2[i].setReal(0);
                    A2[i].setImaginary(0);
                }
            }
            for (int i = end; i < A.Length; i++)
            {
                A[i].setReal(0);
                A[i].setImaginary(0);

                if (samples2 != null)
                {
                    A2[i].setReal(0);
                    A2[i].setImaginary(0);
                }
            }

            Fourier.Inverse(A, ref filter, form);

            Convolution(ref samples, filter);

            if(samples2 != null)
            {
                Fourier.Inverse(A2, ref filter2, form);

                Convolution(ref samples2, filter2);
            }

            return true;
        }

        /*
        * LPass
        *
        *  Low Pass filter function, takes references to the DFT(A) and samples and filters them based on user selection
        * 
        * Parameter:
        *          A        reference to an array of Complex numbers that holds the DFT
        *          s        refernce to the array of samples
        *          bucket   bucket selected on the DFT graph
        *          form     Form1 (main form)
        *
        * Return bool returns false if A[0] is selected
        */
        public static bool LPass(ref Complex[] A, ref Complex[] A2, ref double[] samples, ref double[] samples2, int bucket, Form1 form)
        {
            int Nyquist = A.Length / 2, Nyquist2 = 0;
            int start, end;
            double[] filter = new double[0];
            double[] filter2 = new double[0];
            A = new Complex[A.Length];

            if (samples2 != null)
            {
                Nyquist2 = A2.Length / 2;
                A2 = new Complex[A2.Length];
            }

            for (int i = 0; i < A.Length; i++)
            {
                A[i] = new Complex();

                if (samples2 != null)
                {
                    A2[i] = new Complex();
                }
            }

            if (bucket == 0)
            {
                return false;
            }

            if (bucket < Nyquist)
            {
                start = bucket;
                end = A.Length - bucket;

            } else  if(bucket > Nyquist)
            {
                start = bucket - Nyquist;
                end = bucket;

            } else
            {
                A[Nyquist].setReal(1);
                A[Nyquist].setImaginary(-1);

                Fourier.Inverse(A, ref filter, form);

                Convolution(ref samples, filter);

                if(samples2 != null)
                {
                    A2[Nyquist].setReal(1);
                    A2[Nyquist].setImaginary(-1);

                    Fourier.Inverse(A2, ref filter2, form);

                    Convolution(ref samples2, filter2);
                }

                return true;
            }

            for(int i = 0; i < start; i++)
            {
                A[i].setReal(1);
                A[i].setImaginary(-1);

                if(samples2 != null)
                {
                    A2[i].setReal(1);
                    A2[i].setImaginary(-1);
                }
            }
            for(int i = end; i < A.Length; i++)
            {
                A[i].setReal(1);
                A[i].setImaginary(-1);

                if(samples2 != null)
                {
                    A2[i].setReal(1);
                    A2[i].setImaginary(-1);
                }
            }

            Fourier.Inverse(A, ref filter, form);

            Convolution(ref samples, filter);

            if(samples2 != null)
            {
                Fourier.Inverse(A2, ref filter2, form);

                Convolution(ref samples2, filter2);
            }

            return true;
        }

        /*
        * Convolution method
        *
        *  Convolutes the filter passed in with the reference to the samples of a wave
        * 
        * Parameter:
        *          samples  refernce to the array of samples
        *          filter   filter built by either LPass or HPass methods
        *
        * Return void
        */
        private static void Convolution(ref double[] samples, double[] filter)
        {
            double[] fSamples = new double[samples.Length + filter.Length], temp = new double[samples.Length];

            System.Array.Copy(samples, fSamples, samples.Length);

            for(int t = 0; t < samples.Length; t++)
            {
                for(int f = 0; f < filter.Length; f++)
                {
                    temp[t] += (fSamples[t + f] * filter[f]);
                }
            }

            samples = temp;
        }
    }
}