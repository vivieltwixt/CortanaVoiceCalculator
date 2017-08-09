using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641
// Rainbow image courtesy of https://openclipart.org/detail/147337/rainbow-swirl-120gon


namespace CortanaVoiceCalculator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string[] colors = { "Red.", "Blue.", "Green.", "Yellow.", "Pink.", "Magenta.", "Cyan.", "Indigo.", "Violet.", "Brown.", "Black.", "White.", "Rainbow.", "Gray.", "Orange.", "Maroon.", "Navy."};
        private string[] infinity = { "Infinity." };
        private Boolean doingColorMath = false;
        private Boolean doingInfinityMath = false;
        private Boolean doingObjectMath = false;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            double operand = 0.0;
            double operand2 = 0.0;
            string voiceCommandName;

            // Pathway followed if program was activated via voice
            if (!string.IsNullOrEmpty(e.Parameter.ToString()))
            {
                if (e.NavigationMode == NavigationMode.New)
                {
                    var result = e.Parameter as SpeechRecognitionResult;
                    var semanticProp = result.SemanticInterpretation.Properties;

                    Boolean firstWordIsRealNumber = canConvertVoiceToNumber(semanticProp["number"][0]);
                    Boolean secondWordIsRealNumber = canConvertVoiceToNumber(semanticProp["number2"][0]);

                    if (firstWordIsRealNumber)
                        operand = convertVoiceToNumber(semanticProp["number"][0]);
                    if (secondWordIsRealNumber)
                        operand2 = convertVoiceToNumber(semanticProp["number2"][0]);

                    voiceCommandName = result.RulePath.FirstOrDefault();                    

                    if(firstWordIsRealNumber && secondWordIsRealNumber)
                        performMathOperation(operand, operand2, semanticProp["commandMode"][0], voiceCommandName);
                }
            }

            await calculate();

            while (true) await listenForRefresh();
        }

        // Listens for voice commanded refresh and then performs refresh
        private async Task listenForRefresh()
        {
            SpeechRecognizer recognizer = new SpeechRecognizer();
            string[] refresh = { "refresh" };
            var listConstraint = new SpeechRecognitionListConstraint(refresh, "Call Refresh Button");

            recognizer.Constraints.Add(listConstraint);
            await recognizer.CompileConstraintsAsync();

            var task = await recognizer.RecognizeAsync();

            string confidence = task.Confidence.ToString();
            if (confidence.Equals("High"))
            {
                answerImageBox.Visibility = Visibility.Collapsed;
                OperationBox.Text = "";
                AnswerBox.Text = "";
                await calculate();
            }
        }

        // Calculates the operators spoken by user
        private async Task calculate()
		{
			SpeechRecognitionResult newSpeech;
			String numString;

			double firstNum = 0;
			double secondNum = 0;

			String firstColor = "Noncolor thing";
            String secondColor = "Noncolor thing";

            String firstObject = "Numeral";
            String secondObject = "Numeral";
			
			// Ask for first number
				newSpeech = await RecognizeFirstNumberSpeech();
				numString = newSpeech.Text;

				// Checks if what sort of math we are doing
				if (colors.Contains(numString))
				{
					firstColor = numString;
					doingColorMath = true;
				}
				else if (infinity.Contains(numString))
				{
                    doingInfinityMath = true;
				}
				else
				{
                    if (canConvertVoiceToNumber(numString))    //If true we're doing number math
                        firstNum = convertVoiceToNumber(numString);
                    else
                    {
                        doingObjectMath = true;
                        firstObject = numString;
                    }                    
				}

			//Ask for operator
				newSpeech = await RecognizeOperatorSpeech();
				String operation = newSpeech.Text;
				String commandName = determineCommandName(operation);

		   //Ask for second number
				newSpeech = await RecognizeSecondNumberSpeech();
				numString = newSpeech.Text;


              // Checks if what sort of math we are doing
                if (colors.Contains(numString))
                {
                    secondColor = numString;
                    doingColorMath = true;
                }
                else if (infinity.Contains(numString))
                {
                    doingInfinityMath = true;
                }
                else
                {
                    if (canConvertVoiceToNumber(numString))   // If true, we're doing number math
                        secondNum = convertVoiceToNumber(numString);
                    else
                    {
                        doingObjectMath = true;
                        secondObject = numString;
                    }
                }

            //Performs appropriate operation depending on operand types
                if (doingInfinityMath && doingColorMath)
                    performColorAndInfinityOperation();
                else if (doingColorMath)
                    performColorOperation(firstColor, secondColor, commandName);
                else if (doingInfinityMath)
                    performInfinityOperations(commandName);
                else if (doingObjectMath)
                    performObjectOperations(firstObject, secondObject,commandName);
                else
                    performMathOperation(firstNum, secondNum, "voice", commandName);
		}

        // Performs "math" operations on object operands
        private async void performObjectOperations(string firstObject, string secondObject, string commandName)
        {
            doingObjectMath = false;
            Random rnd = new Random();
            int dice = rnd.Next(1, 11); //Gets random number from 1 to 10 inclusive
            
            firstObject = removeEndPoint(firstObject);
            secondObject = removeEndPoint(secondObject);

            switch (commandName)
            {
                case "Multiplication":
                    OperationBox.Text = firstObject + " x " + secondObject;
                    break;
                case "Division":
                    OperationBox.Text = firstObject + " / " + secondObject;
                    break;
                case "Addition":
                    OperationBox.Text = firstObject + " + " + secondObject;
                    break;
                case "Subtraction":
                    OperationBox.Text = firstObject + " - " + secondObject;
                    break;
                default:
                    break;
            }

            String voiceAnswer = getRandomResponse(dice);
            AnswerBox.Text = voiceAnswer;

            await SpeakText(voicePlaybackElement, voiceAnswer);    
        }

        // Returns a witty response when given a number from 1 to 10 inclusive
        private string getRandomResponse(int dice)
        {
            switch (dice)
            {
                case 1:
                    return "To be? or not to be? That is often the real question.";
                case 2:
                    return "How is a raven like writing desk? The answer is the same for both.";
                case 3:
                    return "A rather poor choice of decor";
                case 4:
                    return "Batman";
                case 5:
                    return "Refer to Quantum Physics for this one.";
                case 6:
                    return "The negative square root of flourescence";
                case 7:
                    return "A teaspoon";
                case 8:
                    return "The derivative of slumber";
                case 9:
                    return "Disney";
                default:
                    return "The circumference of curiosity";
            }
        }

        // Performs "math" operations on infinity operands
        private async void performInfinityOperations(string commandName)
        {
            doingInfinityMath = false;
                     
            string voiceAnswer = "Attempted arithmetic with infinity. Disruption of the space time " +
                   "continuum will now ensue. Thank you for your time.";

            OperationBox.Text = "     \u221E\u221E\u221E\u221E\u221E\u221E\u221E\u221E\u221E\u221E\u221E";
            AnswerBox.Text = voiceAnswer;

            await SpeakText(voicePlaybackElement, voiceAnswer);            
        }

        // Performs "math" operation in the case of color and infinity operands
        private async void performColorAndInfinityOperation()
        {
            doingColorMath = false;
            doingInfinityMath = false;

            answerImageBox.Visibility = Visibility.Visible;

            await SpeakText(voicePlaybackElement, "Quantum rainbow");         
        }
        
        // Removes the period at the end of string
        private String removeEndPoint(String word)
        {
            // Removes the period at the end of string
            if (word.EndsWith("."))
            {
                int lastIndex = word.LastIndexOf(".");
                word = word.Remove(lastIndex, 1);
            }

            return word;
        }

        // Performs "math" operations on color operands
        private async void performColorOperation(string firstColor, string secondColor, string commandName)
        {
            string answer = "rainbow";
            doingColorMath = false;

            firstColor = removeEndPoint(firstColor);
            secondColor = removeEndPoint(secondColor);

            if (firstColor.Equals("Noncolor thing"))
                answer = secondColor;
            else if (secondColor.Equals("Noncolor thing"))
                answer = firstColor;
            else
            {
                switch (firstColor)
                {
                    case "Red":
                        if (secondColor.Equals("Blue")) answer = "Purple";
                        else if (secondColor.Equals("Green")) answer = "Brown";
                        else if (secondColor.Equals("Yellow")) answer = "Orange";
                        break;
                    case "Blue":
                        if (secondColor.Equals("Yellow")) answer = "Green";
                        else if (secondColor.Equals("Red")) answer = "Purple";
                        else if (secondColor.Equals("Green")) answer = "Blue-green";
                        break;
                    case "Green":
                        if (secondColor.Equals("Yellow")) answer = "Light Green";
                        else if (secondColor.Equals("Bed")) answer = "Brown";
                        else if (secondColor.Equals("blue")) answer = "Blue-green";
                        break;
                    case "Yellow":
                        if (secondColor.Equals("Green")) answer = "Light green";
                        else if (secondColor.Equals("Red")) answer = "Orange";
                        else if (secondColor.Equals("Blue")) answer = "Green";
                        break;
                    default:
                        answer = "What an interesting combination!";
                        break;
                }
            }

            switch (commandName)
            {
                case "Multiplication":
                    OperationBox.Text = firstColor + " x " + secondColor;
                    break;
                case "Division":
                    OperationBox.Text = firstColor + " / " + secondColor;
                    answer = "There is not a name in your language for that color.";
                    break;
                case "Addition":
                    OperationBox.Text = firstColor + " + " + secondColor;
                    break;
                case "Subtraction":
                    OperationBox.Text = firstColor + " - " + secondColor;
                    answer = "A piece of the UV spectrum, I suppose.";
                    break;
                default:
                    break;
            }

            AnswerBox.Text = answer;

            await SpeakText(voicePlaybackElement, answer);
        }

        // Checks if input is a number
        private Boolean canConvertVoiceToNumber(string word)
        {
            double number = 0.0;

            word = word.Replace(" ", "");
            word = removeEndPoint(word);
            word = convertPointToDecimal(word);

            if (word.Equals("One"))      // Neccessary because voice recognition recognizes '1' as the word 'one' instead of the number '1'
                return true;
            else
            {
                try { number = Convert.ToDouble(word); }
                catch (FormatException) { return false; }
                catch (OverflowException) { return false; }
            }

            return true;
        }

        // Converts input to a double
        private double convertVoiceToNumber(string word)
        {
            double number = 0.0;

            word = word.Replace(" ", "");
            word = removeEndPoint(word);
            word = convertPointToDecimal(word);

            if (word.Equals("One"))   // Neccessary because voice recognition recognizes '1' as the word 'one' instead of the number '1'
                number = 1;
            else
                number = Convert.ToDouble(word);

            return number;
        }

        // Converts a the word "point" in a string to a decimal
        private string convertPointToDecimal(string word)
        {
            // Converts word 'point' into decimal
            if (word.Contains("point") || word.Contains("Point"))
            {
                word = word.ToLower();
                word = word.Replace("point", ".");
            }

            return word;
        }

        // Performs appropriate math operation on number operands
        private async void performMathOperation(double num1, double num2, string commandMode, string commandName)
        {
            Boolean wantsToDivideByZero = false;

            string voiceCommandName = commandName;
            string num1String = ConvertDoubleToString(num1);
            string num2String = ConvertDoubleToString(num2);

            double answer = 0.0;

            switch (voiceCommandName)
            {
                case "Multiplication":
                    OperationBox.Text = num1String + " x " + num2String;
                    answer = num1 * num2;
                    break;
                case "Division":
                    OperationBox.Text = num1String + " / " + num2String;
                    if (num2 == 0) wantsToDivideByZero = true;
                    else answer = num1 / num2;
                    break;
                case "Addition":
                    OperationBox.Text = num1String + " + " + num2String;
                    answer = num1 + num2;
                    break;
                case "Subtraction":
                    OperationBox.Text = num1String + " - " + num2String;
                    answer = num1 - num2;
                    break;
                default:
                    break;
            }

            String voiceAnswer = " ";

            if (!wantsToDivideByZero) voiceAnswer = ConvertDoubleToString(answer);
            else
            {
                wantsToDivideByZero = false;
                voiceAnswer = "Attempted to divide by zero. Disruption of the space time " +
                    "continuum will now ensue. Thank you for your time.";
            }

            AnswerBox.Text = voiceAnswer;

            if (commandMode == "voice")
            {
                await SpeakText(voicePlaybackElement, voiceAnswer);
            }
        }

        // Converts a string to a double
        private string ConvertDoubleToString(double answer)
        {
            String vocalResponse = " ";

            try
            {
                vocalResponse = Convert.ToString(answer);
            }
            catch (Exception ex)
            {
                MessageDialog message = new MessageDialog("Unable to convert double to String." + ex.Message);
                message.ShowAsync();
            }
            return vocalResponse;
        }

        // Causes app to speak aloud given text
        private async Task SpeakText(MediaElement audioPlayer, string textToSpeak)
        {
            SpeechSynthesizer synthesizer = new SpeechSynthesizer();

            SpeechSynthesisStream ttsStream = await synthesizer.SynthesizeTextToStreamAsync(textToSpeak);

            audioPlayer.SetSource(ttsStream, "");
        }

        // Sppech recognizer UI for obtaining first operand
        private async Task<SpeechRecognitionResult> RecognizeFirstNumberSpeech()
        {
            SpeechRecognizer recognizer = new SpeechRecognizer();

            SpeechRecognitionTopicConstraint looseConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "number");

            recognizer.UIOptions.AudiblePrompt = "Give me your first number";
            recognizer.UIOptions.ExampleText = "State a number. Say point for the decimal. Try colors and objects.";

            recognizer.Constraints.Add(looseConstraint);

            await recognizer.CompileConstraintsAsync();

            //Put up UI and recognize user's utterance
            SpeechRecognitionResult result = await recognizer.RecognizeWithUIAsync();

            return result;
        }

        // Sppech recognizer UI for obtaining second operand
        private async Task<SpeechRecognitionResult> RecognizeSecondNumberSpeech()
        {
            SpeechRecognizer recognizer = new SpeechRecognizer();
            SpeechRecognitionTopicConstraint looseConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "number");

            recognizer.UIOptions.AudiblePrompt = "Give me your second number";
            recognizer.UIOptions.ExampleText = "State a number. Say point for the decimal. Try colors and objects.";

            recognizer.Constraints.Add(looseConstraint);

            await recognizer.CompileConstraintsAsync();

            //Put up UI and recognize user's utterance
            SpeechRecognitionResult result = await recognizer.RecognizeWithUIAsync();

            return result;
        }

        // Sppech recognizer UI for obtaining operator
        private async Task<SpeechRecognitionResult> RecognizeOperatorSpeech()
        {
            SpeechRecognizer recognizer = new SpeechRecognizer();
            string[] operations = { "times", "multiplied by", "divided by", "plus", "added to", "minus", "subtract" };
            var listConstraint = new SpeechRecognitionListConstraint(operations, "Operators");

            recognizer.UIOptions.ExampleText = @"State the operation type Ex. ""times"", ""divided by"", ""plus"", ""minus""";

            recognizer.Constraints.Add(listConstraint);
            await recognizer.CompileConstraintsAsync();

            SpeechRecognitionResult result = await recognizer.RecognizeWithUIAsync();
            return result;
        }

        // Returns correct math operation category for the command
        private String determineCommandName(String operation)
        {
            if (operation.Equals("times") || operation.Equals("multiplied by"))
            {
                return "Multiplication";
            }
            else if (operation.Equals("minus") || operation.Equals("subtract"))
            {
                return "Subtraction";
            }
            else if (operation.Equals("plus") || operation.Equals("added to"))
            {
                return "Addition";
            }
            else
            {
                return "Division";
            }
        }
        
        // Refreshes screen when refresh button is clicked
        private async void resfreshButton_Click(object sender, RoutedEventArgs e)
        {
            answerImageBox.Visibility = Visibility.Collapsed;
            OperationBox.Text = "";
            AnswerBox.Text = "";
            await calculate();
        }
    }
}
