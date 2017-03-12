using System;

namespace WaveAnalyer
{
    class Windowing
    {
      /*
       * Triangular windowing function
       *
       *  Creates a triangular window based from user selected input
       * 
       * Parameter:
       *          samples  refernce to the array of samples
       *          N        Number of samples to be windowed
       *
       * Return void
       */
        public static void Triangle(ref double[] samples, double N)
        {
            double[] w = new double[(int)N];

            for(int i = 0; i < N; i++)
            {
                w[i] = (2 / N) * (2 / N - Math.Abs(i - (N-1)/ 2));
            }
            int j = 0;
            for(int i = 0; i < samples.Length;)
            {
                samples[i++] *= w[j++];
                if(j == N) { j = 0; }
            }
        }

        /*
       * Hamming windowing function
       *
       *  Creates a Hamming window based from user selected input
       * 
       * Parameter:
       *          samples  refernce to the array of samples
       *          N        number of samples to be windowed
       *
       * Return void
       */
        public static void Hamming(ref double[] samples, double N)
        {
            double[] w = new double[(int)N];

            for (int i = 0; i < N; i++)
            {
                w[i] = 0.538836 - 0.46164 * Math.Cos(2 * Math.PI * i / (N - 1));
            }

            int j = 0;
            for (int i = 0; i < samples.Length;)
            {
                samples[i++] *= w[j++];
                if (j == N) { j = 0; }
            }
        }
    }
}