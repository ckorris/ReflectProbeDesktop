using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReflectProbeDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow _instance;

        private const int BAUD_RATE = 921600;

        private SerialPort _serialPort;

        private const string MAGIC_SAMPLE_NUMBERS = "12345";
        private static string incompleteData = string.Empty;

        private static string COM_PORT = "COM5";

        public MainWindow()
        {
            _instance = this;

            InitializeComponent();

            UpdateDebugInfo("Initializing.");

            _serialPort = new SerialPort(COM_PORT);

            _serialPort.BaudRate = BAUD_RATE;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.DataBits = 8;
            _serialPort.Handshake = Handshake.None;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            _serialPort.Open();
        }

        public static void UpdateDebugInfo(string info)
        {
            //Ensure the method runs on the UI thread.
            _instance.Dispatcher.Invoke(() =>
            {
                _instance.DebugTextBlock.Text = info;
            });
        }

        public static void UpdateValues(int[] values)
        {
            _instance.Dispatcher.Invoke(() =>
            {
                _instance.BarChartCanvas.Children.Clear(); 

                double canvasWidth = _instance.BarChartCanvas.ActualWidth;
                double canvasHeight = _instance.BarChartCanvas.ActualHeight;
                double barWidth = canvasWidth / values.Length; 

                for (int i = 0; i < values.Length; i++)
                {
                    double barHeight = (values[i] / 255.0) * canvasHeight; 

                    //Create a rectangle for each value.
                    Rectangle rect = new Rectangle
                    {
                        Width = barWidth - 2, //Subtract 2 to leave some space between bars.
                        Height = barHeight,
                        Fill = new SolidColorBrush(Colors.Blue) 
                    };

                    Canvas.SetBottom(rect, 0); //Align the base of the rectangle to the bottom of the canvas.
                    Canvas.SetLeft(rect, i * barWidth); //Position each rectangle.

                    _instance.BarChartCanvas.Children.Add(rect);

                    //Create a TextBox for each value, placed above each bar.
                    TextBox textBox = new TextBox
                    {
                        Text = values[i].ToString(),
                        Width = barWidth - 2, //Match the bar's width
                        Background = new SolidColorBrush(Colors.Transparent), //Optional: make background transparent.
                        BorderBrush = new SolidColorBrush(Colors.Transparent), //Optional: remove border.
                        TextAlignment = TextAlignment.Center, //Center the text.
                        IsReadOnly = true //Make it readonly if it's just for display.
                    };

                    //Position the TextBox just above the bar.
                    Canvas.SetBottom(textBox, barHeight + 1); //+1 or more to ensure it's above the bar.
                    Canvas.SetLeft(textBox, i * barWidth);

                    _instance.BarChartCanvas.Children.Add(textBox);
                }
            });
        }

        private static void DataReceivedHandler(
                    object sender,
                    SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();

            // Append the incoming data to any previously incomplete data.
            incompleteData += indata;

            int endIndex = incompleteData.IndexOf("\n");
            while (endIndex >= 0) // While there are complete messages in the buffer
            {
                // Extract the complete message including the ending newline
                string completeMessage = incompleteData.Substring(0, endIndex + 1);
                // Remove the processed message from the buffer
                incompleteData = incompleteData.Substring(endIndex + 1);

                // Process the complete message here
                ProcessMessage(completeMessage);

                // Look for the next complete message
                endIndex = incompleteData.IndexOf("\n");
            }


            /*
            //Check if the beginning of the string is the magic number, 12345, to indicate a sample.
            if (indata.Length < MAGIC_SAMPLE_NUMBERS.Length)
            {
                UpdateDebugInfo(indata);
                return;
            }

            string magicNumberSlot = indata.Substring(MAGIC_SAMPLE_NUMBERS.Length);
            
            if(magicNumberSlot == MAGIC_SAMPLE_NUMBERS)
            {
                int[] values = ExtractIntsFromValuesIncludingMagicNumbers(indata);

                UpdateValues(values);
            }
            else
            {
                UpdateDebugInfo(indata);
            }
            */
        }

        private static void ProcessMessage(string completeMessage)
        {
            if (completeMessage.StartsWith(MAGIC_SAMPLE_NUMBERS))
            {
                int[] values = ExtractIntsFromValuesIncludingMagicNumbers(completeMessage);
                UpdateValues(values);
            }
            else
            {
                UpdateDebugInfo(completeMessage);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q)
            {
                _serialPort.Close();
                this.Close();
            }
        }

        private static int[] ExtractIntsFromValuesIncludingMagicNumbers(string message)
        {
            string[] parts = message.Split('-');

            int[] sensorValues = new int[parts.Length - 2]; // Subtract 2 because the first part is the constant and the last part is empty

            //Parse the remaining values, starting after the magic numbers.
            for (int i = 1; i < parts.Length - 1; i++)
            {

                if (Int32.TryParse(parts[i], out int value))
                {
                    sensorValues[i - 1] = value;
                }
                else
                {
                    Console.WriteLine($"Failed to parse value at index {i}");
                    // Handle parsing failure if necessary

                }
            }

            return sensorValues;
        }
    }
}
