using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace homework6
{
    public partial class Form1 : Form
    {

        TextFieldParser fileCsv = null;
        List<Packet> mySet = null;

        EditableRectangle r1;
        EditableRectangle r2;
        Bitmap b;
        Graphics g;

        int sample_size = 10;
        int num_samples = 100;

        Random rand = new Random();

        double[][] all_samples;

        double[] means;
        double[] variances;

        SortedDictionary<Interval, int> dist_mean;
        SortedDictionary<Interval, int> dist_variance;

        public Form1()
        {
            InitializeComponent();
            compute_distribution();
            initialize_graphics();
        }

        private void initialize_graphics()
        {
            //timer1.Start();
            b = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            g = Graphics.FromImage(b);

            r1 = new EditableRectangle(20, 20, pictureBox1.Width / 2, pictureBox1.Height / 2, pictureBox1, this);
            r2 = new EditableRectangle((pictureBox1.Width / 2) + 20, (pictureBox1.Height / 2) + 20, pictureBox1.Width / 2, pictureBox1.Height / 2, pictureBox1, this);
        }
        private void compute_distribution()
        {
            if (fileCsv != null)
            {
                fileCsv.Close();
            }

            string Path = "C:/Users/giorgia.darmiento/source/repos/homework6/wireshark.csv";
            fileCsv = new TextFieldParser(Path);
            fileCsv.Delimiters = new string[] { "," };

            fileCsv.ReadFields(); // reading the first line

            mySet = new List<Packet>();

            while (!fileCsv.EndOfData)
            {
                string[] row = fileCsv.ReadFields();

                for (int i = 0; i < row.Length; i++)
                {
                    row[i] = row[i].Trim('"');
                }

                int no = Convert.ToInt32(row[0]);
                double time = Convert.ToDouble(row[1]);
                string source = Convert.ToString(row[2]);
                string destination = Convert.ToString(row[3]);
                string protocol = Convert.ToString(row[4]);
                int length = Convert.ToInt32(row[5]);
                string information = Convert.ToString(row[6]);

                Packet packet = new Packet(no, time, source, destination, protocol, length, information);

                mySet.Add(packet);
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            sample_size = trackBar1.Value;
            string labeltext = "Size of sample : " + sample_size.ToString();
            label1.Text = labeltext;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            num_samples = trackBar2.Value;
            string labeltext = "Number of samples : " + num_samples.ToString();
            label2.Text = labeltext;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            richTextBox1.Clear();
            timer1.Stop();

            all_samples = new double[num_samples][];

            for (int i = 0; i < num_samples; i++)
            {
                double[] single_sample = new double[sample_size];

                for (int j = 0; j < sample_size; j++)
                {
                    int ri = rand.Next(0, mySet.Count);
                    Packet p = mySet[ri];

                    single_sample[j] = (double)p.length;
                }
                all_samples[i] = single_sample;
            }

            // compute the means
            // compute the variances
            means = new double[num_samples];
            variances = new double[num_samples];
            for (int h = 0; h < num_samples; h++)
            {
                means[h] = compute_mean(all_samples[h]);
                variances[h] = compute_variance(all_samples[h]);
            }

            // compute mean distribution
            compute_mean_distribution();
            //end

            //compute variance distribution
            compute_variance_distribution();
            //end

            timer1.Start();

            //expected value of the distribution vs expected value of the means
            List<double> all_sizes_l = new List<double>();
            foreach (Packet p in mySet)
            {
                all_sizes_l.Add(p.length);
            }
            double[] all_sizes = all_sizes_l.ToArray();

            double population_expected_value = compute_mean(all_sizes);
            double samplingmean_expected_value = compute_mean(means);
            double samplingvariance_expected_value = compute_mean(variances);

            double population_variance = compute_variance(all_sizes);
            double samplingmean_variance = compute_variance(means);
            double samplingvariance_variance = compute_variance(variances);

            richTextBox1.AppendText("Mean of the entire population: " + population_expected_value.ToString().PadRight(50));
            richTextBox1.AppendText("Mean of the sampling mean: " + samplingmean_expected_value.ToString().PadRight(50));
            richTextBox1.AppendText("Mean of the sampling variance: " + samplingvariance_expected_value.ToString().PadRight(50) + "\n");

            richTextBox1.AppendText("Variance of the entire population: " + population_variance.ToString().PadRight(50));
            richTextBox1.AppendText("Variance of the sampling mean: " + samplingmean_variance.ToString().PadRight(50));
            richTextBox1.AppendText("Variance of the sampling variance: " + samplingvariance_variance.ToString().PadRight(50) + "\n");
            //end

        }

        public double compute_mean(double[] numbers)
        {
            int count = 1;
            double mean = 0;
            int size = numbers.Length;
            for (int i = 0; i < size; i++)
            {
                mean += ((numbers[i] - mean) / (double)count);
                count++;
            }
            return mean;
        }


        public double compute_variance(double[] numbers)
        {
            double mean = 0;
            double oldM;
            double variance = 0;
            int count = 1;
            int size = numbers.Length;

            for (int j = 0; j < size; j++)
            {
                double val = numbers[j];
                oldM = mean;
                mean += (val - mean) / count;
                variance += (val - mean) * (val - oldM);
                count++;
            }

            variance /= (numbers.Length);

            return (variance);
        }

        public void compute_mean_distribution()
        {
            double mean_min = means.Min();
            int min_m = (int)Math.Floor(mean_min);

            int interval_size = 20;
            int next = min_m + interval_size;

            dist_mean = new SortedDictionary<Interval, int>();

            Interval inter = new Interval(min_m, next);
            dist_mean[inter] = 0;

            for (int h = 0; h < means.Length; h++)
            {
                bool inserted = false;

                List<Interval> keys = dist_mean.Keys.ToList();
                foreach (Interval v in keys)
                {
                    if (means[h] >= v.down && means[h] < v.up)
                    {
                        inserted = true;
                        dist_mean[v]++;
                    }
                }
                while (!inserted)
                {
                    Interval newint = new Interval(next, next + interval_size);
                    next = next + interval_size;
                    dist_mean[newint] = 0;

                    if (means[h] >= newint.down && means[h] < newint.up)
                    {
                        dist_mean[newint]++;
                        inserted = true;
                    }
                }
            }
        }

        public void compute_variance_distribution()
        {
            double variance_min = variances.Min();
            int min_v = (int)Math.Floor(variance_min);

            int interval_size_v = 10000;
            int next_v = min_v + interval_size_v;

            dist_variance = new SortedDictionary<Interval, int>();

            Interval inter_v = new Interval(min_v, next_v);
            dist_variance[inter_v] = 0;

            for (int k = 0; k < variances.Length; k++)
            {
                bool inserted = false;

                List<Interval> keys = dist_variance.Keys.ToList();
                foreach (Interval v in keys)
                {
                    if (variances[k] >= v.down && variances[k] < v.up)
                    {
                        inserted = true;
                        dist_variance[v]++;
                    }
                }
                while (!inserted)
                {
                    Interval newint = new Interval(next_v, next_v + interval_size_v);
                    next_v = next_v + interval_size_v;
                    dist_variance[newint] = 0;

                    if (variances[k] >= newint.down && variances[k] < newint.up)
                    {
                        dist_variance[newint]++;
                        inserted = true;
                    }
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            g.Clear(pictureBox1.BackColor);
            drawHistogram_mean();
            drawHistogram_variance();
            pictureBox1.Image = b;
        }

        private void drawHistogram_mean()
        {

            g.FillRectangle(Brushes.Black, r1.r);
            g.DrawRectangle(Pens.Red, r1.r);

            int maxvalue = dist_mean.Values.Max();

            int space_height = r1.r.Bottom - r1.r.Top - 20;
            int space_width = r1.r.Right - r1.r.Left - 20;

            int num_intervals = dist_mean.Keys.Count;
            int histrect_width = space_width / num_intervals;

            int start = r1.r.X;

            foreach (KeyValuePair<Interval, int> k in dist_mean)
            {
                int rect_height = (int)(((double)k.Value / (double)maxvalue) * ((double)space_height));
                Rectangle hist_rect = new Rectangle(start, r1.r.Bottom - rect_height, histrect_width, rect_height);

                g.FillRectangle(Brushes.Green, hist_rect);
                g.DrawRectangle(Pens.White, hist_rect);

                //scrittura chiavi

                Interval interval_k = k.Key;
                string text = interval_k.ToString();
                Rectangle stringPos = new Rectangle(start, r1.r.Bottom + 20, histrect_width, histrect_width + 20);
                Font font1 = new Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Pixel);

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                Font goodFont = findFont(g, text, stringPos.Size, font1);

                g.DrawString(text, goodFont, Brushes.Black, stringPos, stringFormat);

                //fine scrittura chiavi

                start += histrect_width;
            }
        }
        private void drawHistogram_variance()
        {
            g.FillRectangle(Brushes.Black, r2.r);
            g.DrawRectangle(Pens.Red, r2.r);

            int maxvalue = dist_variance.Values.Max();

            int space_height = r2.r.Bottom - r2.r.Top - 20;
            int space_width = r2.r.Right - r2.r.Left - 20;

            int num_intervals = dist_variance.Keys.Count;
            int histrect_width = space_width / num_intervals;

            int start = r2.r.X;

            foreach (KeyValuePair<Interval, int> k in dist_variance)
            {
                int rect_height = (int)(((double)k.Value / (double)maxvalue) * ((double)space_height));
                Rectangle hist_rect = new Rectangle(start, r2.r.Bottom - rect_height, histrect_width, rect_height);

                g.FillRectangle(Brushes.Green, hist_rect);
                g.DrawRectangle(Pens.White, hist_rect);

                //scrittura chiavi

                Interval interval_k = k.Key;
                string text = interval_k.ToString();
                Rectangle stringPos = new Rectangle(start, r2.r.Bottom + 20, histrect_width, histrect_width + 20);
                Font font1 = new Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Pixel);

                StringFormat stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                Font goodFont = findFont(g, text, stringPos.Size, font1);

                g.DrawString(text, goodFont, Brushes.Black, stringPos, stringFormat);

                //fine scrittura chiavi

                start += histrect_width;
            }
        }

        private Font findFont(Graphics g, string myString, Size Room, Font PreferedFont)
        {
            SizeF RealSize = g.MeasureString(myString, PreferedFont);
            float HeightScaleRatio = Room.Height / RealSize.Height;
            float WidthScaleRatio = Room.Width / RealSize.Width;

            float ScaleRatio = (HeightScaleRatio < WidthScaleRatio) ? ScaleRatio = HeightScaleRatio : ScaleRatio = WidthScaleRatio;

            float ScaleFontSize = PreferedFont.Size * ScaleRatio;

            return new Font(PreferedFont.FontFamily, ScaleFontSize);
        }
    }
}