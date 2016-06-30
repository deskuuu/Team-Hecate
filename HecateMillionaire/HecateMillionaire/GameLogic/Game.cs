﻿namespace HecateMillionaire.GameLogic
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Media;
    using System.Text;
    using System.Threading;

    using Contracts;
    using HecateMillionaire.BaseTable;
    using Jokers;
    using Players;
    using Questions;

    public class Game : IGame, ISound
    {
        // start game - method to be called from Main
        // ask for player's name and color
        // initilize game - create player, load questions
        // play game - show the question, ask the player for his choice, set timer
        // check if it's correct, if it's not - game over, otherwise ask next question
        // game offers a joker - if player can't answer in time ?
        // end of game - show player's scores, show players statistics 
        // ask for new game
        // private static instance of the same class
        private static readonly Game GameInstance = null;
        private static List<Question> questions;
        private static Player player;
        private static int wrongAnswers;
        private static bool isSkippedQuestion;
        private static bool isUnlockJoker;

        static Game()
        {
            // create the instance only if the instance is null
            GameInstance = new Game();
        }

        public static Game GetInstance()
        {
            // return the already existing instance
            return GameInstance;
        }

        // singleton pattern
        // private constructor to restrict the game creation from outside
        private Game()
        {
        }

        // methods from ISound
        public void PlayGameOverSound()
        {
            using (SoundPlayer soundPlayer = new SoundPlayer(GameConstants.SoundGameOver))
            {
                // Use PlaySync to load and then play the sound.
                // The program will pause until the sound is complete.
                soundPlayer.PlaySync();
            }
        }

        // methods from IGame
        public void StartGame()
        {
            // load game logo and menu
            this.InitiliazeGame();
        }

        public void InitiliazeGame()
        {
            Console.Title = "~ Hecate Millionaire ~";

            // load game image and sound
            LoadImage(GameConstants.FileHecateStart);
            this.PlayStartSound();

            // initialize player and questions
            player = new Player();
            //questions = Game.InitializeQuestions(GameConstants.FileQuestions);
            questions = Game.InitializeQuestionsWithLevels(GameConstants.FileQuestions);
            wrongAnswers = 0;

            this.LoadMainMenu();
        }

        public void PlayGame()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.BackgroundColor = ConsoleColor.Black;

            // 17.6.2016, Kristina. Добавена е проверка за коректност на отговора
            for (int i = 0; i < questions.Count; i++)
            {
                char answer;
                bool flag = false;
                Question currentQuestion = questions[i];
                

                // use infinitely loop because of jokers
                while (true)
                {
                    Console.Clear(); // clear console

                    Console.WriteLine(currentQuestion);
                    Console.WriteLine(currentQuestion.PrintAnswers(flag)); // print answers

                    //check for level
                    if (currentQuestion.GetType().Name.Equals("QuestionLevel2"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("BONUS!You unlock the jokers!");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        QuestionLevel2 currentQuestionLevel2 = currentQuestion as QuestionLevel2;
                        isUnlockJoker = true;
                    }
                    if (currentQuestion.GetType().Name.Equals("QuestionLevel3"))
                    {
                        QuestionLevel3 currentQuestionLevel3 = currentQuestion as QuestionLevel3;

                        if (!isSkippedQuestion)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("BONUS!You can skip one question!");
                            Console.ForegroundColor = ConsoleColor.Magenta;

                            Console.Write("Do you want to skip this question - y/n :");
                            var skipChoice = Char.Parse(Console.ReadLine());

                            if (skipChoice == 'y' || skipChoice == 'Y')
                            {
                                Console.Clear();
                                isSkippedQuestion = true;
                                QuestionLevel3 nextQuestion = currentQuestionLevel3.SkipQuestion(currentQuestionLevel3, questions);
                                currentQuestionLevel3 = nextQuestion;

                                //print next question
                                Console.Clear(); //clear console
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Next question :");
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine(currentQuestionLevel3);
                                Console.WriteLine(currentQuestionLevel3.PrintAnswers(flag)); //print answers
                            }
                        }
                    }

                    if (isUnlockJoker)
                    {
                        this.OfferJoker(); // Print jokers
                    }
                    
                    //timer for answer
                    answer = DisplayTime.CreateTimer(currentQuestion.TimerSeconds);

                    // answer = Char.Parse(Console.ReadLine()); take char answer

                    // chek for use joker
                    if (answer > '0' && answer <= '3')
                    {
                        // for print only two answers when use FiftyFifty joker or print another joker
                        flag = this.UseJoker(answer, currentQuestion.RightAnswerIndex, currentQuestion.Answers);
                    }
                    else
                    {
                        break;
                    }
                }

                if (answer == default(char))
                {
                    continue;
                }

                QuestionChecker checker = new QuestionChecker(currentQuestion, answer);
                if (checker.Tell())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Your answer is true");
                    this.PlayCorrectSound();

                    // Add 100 scores if the answaer is right
                    player.Score += currentQuestion.QuestionScore;

                    Console.WriteLine("SCORE : {0} ", player.Score);
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Thread.Sleep(500); // white because of information
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("You are wrong");
                    this.PlayWrongSound();
                    wrongAnswers++;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Thread.Sleep(500); // white because of information

                    // game over if 3 wrong questions
                    if (wrongAnswers == GameConstants.MaxWrongAnswers)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("You have 3 wrong answers !");
                        Thread.Sleep(500);
                        break;
                    }
                }
                if (isSkippedQuestion)
                {
                    currentQuestion = questions[i++];
                }
                // Край на промените на Кристина
            }

            this.EndGame();
        }

        public bool CheckPlayerAnswer(char answer)
        {
            return false;
        }

        public void OfferJoker()
        {
            var listJoker = player.Jokers;

            Console.WriteLine();
            Console.WriteLine("Jokers:");

            for (int j = 0; j < listJoker.Count; j++)
            {
                if (listJoker[j].IsUsed != true)
                {
                    System.Console.WriteLine(j + 1 + " -> " + listJoker[j].Type);
                }
                else
                {
                    System.Console.BackgroundColor = ConsoleColor.Cyan;
                    System.Console.WriteLine(j + 1 + " -> " + listJoker[j].Type);
                    System.Console.BackgroundColor = ConsoleColor.Black;
                }
            }
        }

        public bool UseJoker(char answer, int rithAnswerIndex, string[] answersOfQuestion)
        {
            bool flag = false; // for print only two answers when use FiftyFifty joker

            switch (answer)
            {
                case '1':
                    if (player.SelectJoker(JokerType.FiftyFifty))
                    {
                        flag = true;
                    }
                    else
                    {
                        flag = false;
                        Thread.Sleep(1000);
                    }

                    break;
                case '2':
                    if (player.SelectJoker(JokerType.HellFromPublic))
                    {
                        var fiftyFifty = player.Jokers[0]; // if used FiftyFifty joker

                        HelpFromPublicJoker help = new HelpFromPublicJoker(JokerType.HellFromPublic);
                        System.Console.WriteLine("\nPublic thing");
                        System.Console.WriteLine(help.Mind(rithAnswerIndex, fiftyFifty.IsUsed, answersOfQuestion));
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }

                    break;
                case '3':
                    if (player.SelectJoker(JokerType.CallFriend))
                    {
                        var fiftyFifty = player.Jokers[0]; // if used FiftyFifty joker

                        System.Console.WriteLine("\nWho friend you want to call!");
                        var friendName = System.Console.ReadLine();
                        CallFriendJoker frient = new CallFriendJoker(JokerType.CallFriend, friendName);
                        System.Console.WriteLine("{0} say: {1}", friendName, frient.Respond(fiftyFifty.IsUsed, answersOfQuestion));
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }

                    break;
                default:
                    break;
            }

            return flag;
        }

        public void EndGame()
        {
            if (this.CheckForWinner())
            {
                string textWin = "YOU'RE A HECATE MILIONAIRE ! - You have {0} lv\n";

                Console.Clear();
                LoadImage(GameConstants.FileChampion);
                this.PlayWinSound();

                var currentCol = (Console.WindowWidth / 2) - (textWin.Length / 2);
                Console.Write(new string(' ', currentCol));

                // Console.WriteLine("\n\tYOU'RE A HECATE MILIONAIRE ! - You have {0} lv\n", player.Score );
                Console.WriteLine(string.Format(textWin, player.Score));

                // save record and name in file when game over
            }
            else
            {
                string textLose = "Do you want to try another game?\n";

                Console.Clear();
                LoadImage(GameConstants.FileGameOver);
                this.PlayGameOverSound();

                var currentCol = (Console.WindowWidth / 2) - (textLose.Length / 2)-5;
                Console.Write(new string(' ', currentCol));

                // Console.WriteLine("\n\tDo you want to try another game?\n");
                Console.WriteLine("You have win {0} lv but you're not THE CHAMPION",player.Score);
                currentCol = (Console.WindowWidth / 2) - (textLose.Length / 2);
                Console.Write(new string(' ', currentCol));
                Console.WriteLine(textLose);
            }
            player.GameOver();

            this.LoadMainMenu();
        }

        public void ShowStatistics()
        {
            // print players results
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.Black;

            TheBestThreePlayers.ShowPlayerResultFileRekord(player.Name);
            Console.WriteLine();
        }

        public void RestartGame()
        {
           this.StartGame();
        }

        private void LoadMainMenu()
        {
            string[] textInformation = new string[]
            {
                "START NEW GAME ?  =>> \n",
                "SHOW BEST PLAYERS ?  =>>\n",
                "EXIT ?  =>>\n"
            };

            string[] textForChoise = new string[]
            {
                "\tPress 'Enter' => for restart and play a new game\n",
                "\tPress 'Space' for close the game and see the result\n",
                "\tPress 'Esc' to close the game."
            };

            Console.ForegroundColor = ConsoleColor.White;

            ConsolePrintText.Print(textInformation);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();

            ConsolePrintText.Print(textForChoise);
            var choice = Console.ReadKey();

            if (choice.Key == ConsoleKey.Enter)
            {
                Console.Clear();

                // the player don't plays for first time
                if (!player.Name.Equals("Player"))
                {
                    player.Score = 0;
                    this.PlayGame();
                }
                else
                {
                    string playerName = string.Empty;
                    do
                    {
                        //if this is the first game of this player
                        //set player and start the game
                        Console.ForegroundColor = ConsoleColor.Red;

                        Console.WriteLine("Player name must be at least 4 symbols\n");
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Red;

                        Console.WriteLine("What's your name?  =>>");
                        Console.BackgroundColor = ConsoleColor.Black;
                        playerName = Console.ReadLine();

                    } while (playerName.Length < 4);

                    player.Name = playerName;
                    PlayGame();
                }
            }
            else if (choice.Key == ConsoleKey.Spacebar)
            {
                // show best players
                this.ShowStatistics();
                this.LoadMainMenu();
            }
            else if (choice.Key == ConsoleKey.Escape)
            {
                Environment.Exit(0);
            }
            else
            {
                throw new ArgumentException("Invalid choice.Try again!");
            }
        }

        // helper methods
        private static List<Question> InitializeQuestions(string file)
        {
            // read text file
            StreamReader reader = new StreamReader(file);
            StringBuilder text = new StringBuilder();
            using (reader)
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    text.Append(line);
                    text.Append(Environment.NewLine);
                    line = reader.ReadLine();
                }
            }

            // parse text to questions
            string[] questions = text.ToString().Split(new string[] { "#" }, StringSplitOptions.RemoveEmptyEntries);

            List<Question> questionsList = new List<Question>();
            for (int i = 0; i < questions.Length; i++)
            {
                string[] currentQuestion = questions[i].Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                string questionText = currentQuestion[0];
                string answersStr = currentQuestion[1];
                int indexRightQuestion = int.Parse(currentQuestion[2]);

                string[] answers = answersStr.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);

                Question question = new Question(questionText, answers, indexRightQuestion - 1);

                questionsList.Add(question);
            }
            return questionsList;
        }

        private static List<Question> InitializeQuestionsWithLevels(string file)
        {
            // read text file
            StreamReader reader = new StreamReader(file);
            StringBuilder text = new StringBuilder();
            using (reader)
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    text.Append(line);
                    text.Append(Environment.NewLine);
                    line = reader.ReadLine();
                }
            }

            // parse text to questions
            //5 questions per level
            //TODO 15-th question is the same as 14-th -> to change it
            string[] questions = text.ToString().Split(new string[] { "#" }, StringSplitOptions.RemoveEmptyEntries);

            List<Question> questionsList = new List<Question>();
            for (int i = 0; i < questions.Length; i++)
            {
                string[] currentQuestion = questions[i].Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                string questionText = currentQuestion[0];
                string answersStr = currentQuestion[1];
                int indexRightQuestion = int.Parse(currentQuestion[2]);

                string[] answers = answersStr.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);

                Question question;
                //first 5 are Level1, next 5 - Level2, next 5 - Level3
                //level1
                if (i < GameConstants.NumberOfQuestionPerLevel)
                {
                    question = new QuestionLevel1(questionText, answers, indexRightQuestion - 1);
                }
                //level2
                else if (i >= GameConstants.NumberOfQuestionPerLevel && i < GameConstants.NumberOfQuestionPerLevel * 2)
                {
                    question = new QuestionLevel2(questionText, answers, indexRightQuestion - 1);
                }
                //lelev3
                else 
                {
                    question = new QuestionLevel3(questionText, answers, indexRightQuestion - 1);
                }
                questionsList.Add(question);
            }
            return questionsList;
        }

        private static void LoadImage(string filepath)
        {
            // Read from file
            string[] lines = File.ReadAllLines(filepath);

            Console.ForegroundColor = ConsoleColor.Red;

            ConsolePrintText.Print(lines);
        }

        private bool CheckForWinner()
        {
            bool isWinner = false;

            if (player.Score == this.GetGameMaxScore())
            {
                isWinner = true;
            }
            return isWinner;
        }

        public void PlayWinSound()
        {
            using (SoundPlayer soundPlayer = new SoundPlayer(GameConstants.SoundWin))
            {
                // Use PlaySync to load and then play the sound.
                // The program will pause until the sound is complete.
                soundPlayer.PlaySync();
            }
        }

        public void PlayCorrectSound()
        {
            using (SoundPlayer soundPlayer = new SoundPlayer(GameConstants.SoundCorrect))
            {
                // Use PlaySync to load and then play the sound.
                // The program will pause until the sound is complete.
                soundPlayer.PlaySync();
            }
        }

        public void PlayWrongSound()
        {
            using (SoundPlayer soundPlayer = new SoundPlayer(GameConstants.SoundWrong))
            {
                // Use Play to bot wait too much time until the sound is complete and load next question
                soundPlayer.Play();
            }
        }

        public void PlayStartSound()
        {
            using (SoundPlayer soundPlayer = new SoundPlayer(GameConstants.SoundStart))
            {
                // Use PlaySync to load and then play the sound.
                // The program will pause until the sound is complete.
                soundPlayer.PlaySync();
            }
        }

        private int GetGameMaxScore()
        {
            int maxScore = GameConstants.NumberOfQuestionPerLevel * GameConstants.QuestionScoreLevel1
                + GameConstants.NumberOfQuestionPerLevel * GameConstants.QuestionScoreLevel2
                + GameConstants.NumberOfQuestionPerLevel * GameConstants.QuestionScoreLevel3;
            return maxScore;
        }
    }
}
