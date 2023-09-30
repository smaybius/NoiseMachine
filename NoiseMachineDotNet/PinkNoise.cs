using System;

namespace NoiseMachineDotNet
{
    /*
     * PinkNoise.java  -  a pink noise generator
     *
     * Copyright (c) 2008, Sampo Niskanen <sampo.niskanen@iki.fi>
     * All rights reserved.
     * Source:  http://www.iki.fi/sampo.niskanen/PinkNoise/
     *
     * Distrubuted under the BSD license:
     *
     * Redistribution and use in source and binary forms, with or without
     * modification, are permitted provided that the following conditions
     * are met:
     *
     *  - Redistributions of source code must retain the above copyright
     * notice, this list of conditions and the following disclaimer.
     * 
     *  - Redistributions in binary form must reproduce the above
     * copyright notice, this list of conditions and the following
     * disclaimer in the documentation and/or other materials provided
     * with the distribution.
     *
     *  - Neither the name of the copyright owners nor contributors may be
     * used to endorse or promote products derived from this software
     * without specific prior written permission.
     *
     * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
     * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
     * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
     * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
     * COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
     * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
     * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
     * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
     * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
     * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
     * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
     * POSSIBILITY OF SUCH DAMAGE.
     */
    internal class PinkNoise
    {
        private readonly int poles;
        private readonly double[] multipliers;

        private readonly double[] values;




        /**
     * Generate pink noise from a specific randomness source
     * specifying alpha and the number of poles.  The larger the
     * number of poles, the lower are the lowest frequency components
     * that are amplified.
     * 
     * @param alpha   the exponent of the pink noise, 1/f^alpha.
     * @param poles   the number of poles to use.
     * @throws IllegalArgumentException  if <code>alpha < 0</code> or
     *                                      <code>alpha > 2</code>.
     */
        public PinkNoise(double alpha, int poles)
        {
            if (alpha is < 0 or > 2)
            {
                throw new ArgumentException("Invalid pink noise alpha = " +
                                   alpha);
            }

            this.poles = poles;
            multipliers = new double[poles];
            values = new double[poles];

            double a = 1;
            for (int i = 0; i < poles; i++)
            {
                a = (i - (alpha / 2)) * a / (i + 1);
                multipliers[i] = a;
            }

            // Fill the history with random values
            for (int i = 0; i < 5 * poles; i++)
            {
                _ = NextValue();
            }
        }
        /**
         * Return the next pink noise sample.
         *
         * @return  the next pink noise sample.
         */
        public double NextValue()
        {
            /*
             * The following may be changed to  rnd.nextDouble()-0.5
             * if strict Gaussian distribution of resulting values is not
             * required.
             */
            double x = RandomExtensions.NextGaussian(Random.Shared);

            for (int i = 0; i < poles; i++)
            {
                x -= multipliers[i] * values[i];
            }
            Array.Copy(values, 0, values, 1, values.Length - 1);
            values[0] = x;

            return x;
        }
    }
}
