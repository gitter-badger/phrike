﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InterviewTest.xaml.cs" company="">
//   
// </copyright>
// <summary>
//   Interaction logic for InterviewTest.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DataAccess;
using DataModel;

namespace Phrike.GroundControl.Views
{
    /// <summary>
    /// Interaction logic for InterviewTest.xaml
    /// </summary>
    public partial class InterviewTest : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterviewTest"/> class.
        /// </summary>
        public InterviewTest()
        {        
            this.InitializeComponent();
        }

        /// <summary>
        /// The button_ click.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            this.SaveData();
        }

        private List<RadioButton> GetRadioButtons()
        {
            var grid = InterviewGrid;
            List<RadioButton> result = new List<RadioButton>();
            foreach(RadioButton rb in FindVisualChildren<RadioButton>(grid))
            {
                result.Add(rb);
            }
            return result;
        }

        /// <summary>
        /// The get interview result.
        /// </summary>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        public List<SurveyResult> GetInterviewResult()
        {
            List<SurveyResult> result = new List<SurveyResult>();
            List<RadioButton> radioButtons = new List<RadioButton>(this.GetRadioButtons());
            int i = 1;

            foreach (RadioButton rb in radioButtons)
            {
                if (rb.IsChecked == true)
                {
                    SurveyResult s = new SurveyResult();
                    int resultValue = i % 5;
                    if (resultValue == 0)
                    {
                        resultValue = 5;
                    }

                    switch (resultValue)
                    {
                        case 1:
                            s.Answer = SurveyAnswer.Perfect; 
                            break;
                        case 2:
                            s.Answer = SurveyAnswer.Good;
                            break;
                        case 3:
                            s.Answer = SurveyAnswer.Gratifying;
                            break;
                        case 4:
                            s.Answer = SurveyAnswer.Bad;
                            break;
                        case 5:
                            s.Answer = SurveyAnswer.Worst;
                            break;
                    }

                    result.Add(s);
                }

                i++;
            }
            return result;
        }

        /// <summary>
        /// The save data.
        /// </summary>
        private void SaveData()
        {
            using (UnitOfWork unitOfWork = new UnitOfWork())
            {
                int i = 0; // !!!!
                List<SurveyResult> resultList = new List<SurveyResult>(this.GetInterviewResult());

                SurveyQuestion[] questionList = unitOfWork.SurveyQuestionRepository.Get().ToArray();
                List<SurveyQuestion> questionList2 = (List<SurveyQuestion>)unitOfWork.SurveyQuestionRepository.Get();
                Test test = unitOfWork.TestRepository.GetByID(2);

                foreach (var q in questionList2)
                {
                    Console.WriteLine(q.Question);
                }

                for (int j = 0; j < questionList.Length; j++)
                {
                    Console.WriteLine(questionList[j]);
                }

                foreach (SurveyResult result in resultList)
                {
                    result.Test = test;
                    result.SurveyQuestion = questionList[i];
                    unitOfWork.SurveyResultRepository.Insert(result);
                    //  unitOfWork.Save();

                    Console.WriteLine(
                        "inserted result {0}, {1}, {2}",
                        result.SurveyQuestion,
                        result.Answer,
                        result.Test);
                    i++;
                    //}


                }
            }
        }

        /// <summary>
        /// The find visual children.
        /// </summary>
        /// <param name="depObj">
        /// The dep obj.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {               
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if ((child != null) && (child is T))
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
