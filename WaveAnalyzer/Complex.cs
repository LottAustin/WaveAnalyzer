using System;

namespace WaveAnalyer
{
    /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
     * Complex Class
     * 
     * Purpose: The class is used to hold the complex numbers 
     * needed for fourier transform.
     * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
    public class Complex
    {
        private double re; // Real component of the complex number
        private double im; // Imaginary component of the complex number

        /*
         * Default Constructor
         */
        public Complex() {
            re = 0;
            im = 0;
        }

        /*
         * Constructor used if the real component is known
         * 
         * Parameters:  real - known real component
         */
        public Complex(double real) {
            re = real;
            im = 0;
        }

        /*
         * Constructor used if both components are known
         * 
         * Parameter:   real - known real component
         *              imagenary - known imagenary component
         */
        public Complex(double real, double imagenary) {
            re = real;
            im = imagenary;
        }

        /*
         * Method is used to to convert from polar form to Cartesian
         * 
         * Parameter:   r
         *              radians
         *              
         * Return:      data - Complex object created by coonverting from
         *                     polar to Cartesian
         */
        public static Complex fromPolar(double r, double radians) {
            Complex data = new Complex(r * Math.Cos(radians), r * Math.Sin(radians));
            return data;
        }

        /*
         * Method is an operator override for addition. Used to make addion of 
         * Complex objects easier
         * 
         * Parameter:   a - a Complex number object
         *              b - a Complex number object
         *              
         * Return:      data - Complex object created by additon of a and b
         */
        public static Complex operator+(Complex a, Complex b) {
            Complex data = new Complex(a.getReal() + b.getReal(), a.getImaginary() + b.getImaginary());
            return data;
        }

        /*
         * Method is an operator override for subtraction. Used to make subtraction of 
         * Complex objects easier
         * 
         * Parameter:   a - a Complex number object
         *              b - a Complex number object
         *              
         * Return:      data - Complex object created by subtraction of a and b
         */
        public static Complex operator-(Complex a, Complex b) {
            Complex data = new Complex(a.getReal() - b.getReal(), a.getImaginary() - b.getImaginary());
            return data;
        }

        /*
         * Method is an operator override for multiplication. Used to make multiplication of 
         * Complex objects easier
         * 
         * Parameter:   a - a Complex number object
         *              b - a Complex number object
         *              
         * Return:      data - Complex object created by multuplication of a and b
         */
        public static Complex operator*(Complex a, Complex b) {
            Complex data = new Complex((a.getReal() * b.getReal()) - (a.getImaginary() * b.getImaginary()),
                (a.getReal() * b.getImaginary()) + (a.getImaginary() * b.getReal()));

            return data;
        }

        /*
         * Method is a getter for the magnitude of the complex number.
         * Magnitude is calculated uing Pythagoras
         *  
         * return Magnitude of the complex number
         */
        public double Magnitude {
            get {
                return Math.Sqrt(re * re + im * im);
            }
        }

        /*
         * Method is a getter for the phase of the complex number.
         * Assumsion Complex number is from a fourier tranform
         *  
         * Return Phase of the complex number
         */
        public double Phase {
            get {
                if (re != 0)
                {
                    return Math.Atan(im / re);
                } else if(im > 0) {
                    return 90;
                }
                else
                {
                    return -90;
                }
            }
        }

        /*
         * Setter for real component
         * 
         * Parameter:   real - real compent for the Compex number object
         */
        public void setReal(double real) {
            re = real;
        }

        /*
         * Getter for real component
         * 
         * Return:  re - real component of a Complex number object
         */
        public double getReal() {
            return re;
        }

        /*
         * Setter for imaginary component
         * 
         * Parameter:   imaginary - imaginary component for the Complex number object
         */
        public void setImaginary(double imaginary) {
            im = imaginary;
        }

        /*
         * Getter for imginary component
         * 
         * Return:  im - imaginary component of the Complex number object
         */
        public double getImaginary() {
            return im;
        }
    }
}
