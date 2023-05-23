using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leorik.Core;
using System.Runtime.CompilerServices;

namespace Leorik.Tuning
{
    internal class FeaturePlot
    {
        public static void Plot8x8FeatureSets(MultipleRegressionSolver solver, float[] baseline, string fileName, string name)
        {
            IEnumerable<float[]> featureSets = solver.Features;
            const int Margin = 12;
            const int Spacing = 4;
            const int Width = 200;
            const int Height = 1120;
            var bitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            using (Graphics gfx = Graphics.FromImage(bitmap))
            {
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                SolidBrush bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(180, 180, 190));
                gfx.FillRectangle(bgBrush, 0, 0, Width, Height);
                SolidBrush txtBrush = new SolidBrush(System.Drawing.Color.Black);
                Font txtFont = new Font("Courier New", 8);

                const int Tables = FeatureTuner.PawnStructureTables + FeatureTuner.MaterialTables;

                int py = Margin;
                RenderDistribution(Margin, py, bitmap, solver.Positions);
                gfx.DrawString(name, txtFont, txtBrush, Margin + 8 + Spacing , py - 2);
                py += (8 + Spacing);

                int iFeature = 0;
                for (int t = 0; t < Tables; t++)
                {
                    gfx.DrawString(FeatureTuner.TableNames[t], txtFont, txtBrush, 8, py-2);
                    for (int y = 0; y < 8; y++)
                    {
                        int px = Margin;
                        py += (8 + Spacing);
                        for (int x = 0; x < 8; x++)
                        {
                            float mg = baseline[iFeature];
                            RenderTile(px, py, bitmap, GetSlice(featureSets, iFeature), mg, iFeature);
                            px += (Spacing/2 + 8);

                            float eg = mg + baseline[iFeature + 1];
                            RenderTile(px, py, bitmap, GetSliceEg(featureSets, iFeature), eg, iFeature);
                            px += (Spacing + 8);
                            iFeature += 2;
                        }
                    }
                    py += Margin;
                }
            }
            bitmap.Save(fileName, ImageFormat.Png);
        }

        private static void RenderDistribution(int x, int y, Bitmap bitmap, IEnumerable<List<Data>> dataSets)
        {
            int[] count = new int[64];
            int i = 0;
            int max = 0;
            foreach(var dataSet in dataSets) 
            {
                max = Math.Max(max, dataSet.Count);
                count[i++] = dataSet.Count;
            }
            for (int sq = 0; sq < 64; sq++)
            {
                int ty = 8 - (sq >> 3);
                int tx = sq & 7;

                float r = count[sq] / (float)max;
                int red = Math.Min(255, (int)(512 * r));
                int green = Math.Min(255, (int)(512 * (1 -r)));
                var color = System.Drawing.Color.FromArgb(red, green, 0);
                bitmap.SetPixel(x + tx, y + ty, color);
            }
        }

        private static void RenderTile(int x, int y, Bitmap bmp, float[] slice, float baseline, int iFeature)
        {
            int ftSq = ((iFeature/2) % 64);
            for (int sq = 0; sq < 64; sq++)
            {
                int ty = 8 - (sq >> 3);
                int tx = sq & 7;
                var color = (sq == ftSq) ? System.Drawing.Color.DarkBlue : GetColor(slice, baseline, sq);
                bmp.SetPixel(x + tx, y + ty, color);
            }
        }

        private static System.Drawing.Color GetColor(float[] slice, float baseline, int index)
        {
            float value = slice[index] - baseline;
            int c = 255 - (int)Math.Min(255, Math.Abs(value));
            if(value < 0)
                return System.Drawing.Color.FromArgb(255, c, 0);
            else
                return System.Drawing.Color.FromArgb(c, 255, 0);
        }

        private static float[] GetSlice(IEnumerable<float[]> featureSets, int iFeature)
        {
            float[] slice = new float[64];
            int i = 0;
            foreach (var featureSet in featureSets)
                slice[i++] = featureSet[iFeature];
            return slice;
        }

        private static float[] GetSliceEg(IEnumerable<float[]> featureSets, int iFeature)
        {
            float[] slice = new float[64];
            int i = 0;
            foreach (var featureSet in featureSets)
                slice[i++] = featureSet[iFeature] + featureSet[iFeature + 1];
            return slice;
        }
    }
}
