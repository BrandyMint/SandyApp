using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utilities;
using Random = UnityEngine.Random;

namespace Games.Arithmetic {
    public class ExerciveGenerator : MonoBehaviour {
        private const int _ATTEMPTS = 100;
        
        [SerializeField] private Text _txtExercive;
        [SerializeField] private Text _txtAnswer;
        [SerializeField] private Color _colorNeutral = Color.white;
        [SerializeField] private Color _colorRight = Color.green;
        [SerializeField] private Color _colorWrong = Color.red;
        [SerializeField] private int _minNumber = 1;
        [SerializeField] private int _maxNumber = 9;
        [SerializeField] private int _maxAnswer = 20;
        [SerializeField] private OP[] _operations = {OP.PLUS, OP.MINUS, OP.MULT}; 

        [Serializable]
        public enum OP {
            PLUS,
            MINUS,
            MULT
        }

        public void Generate(int answersCount, out int rightAnswer, out IEnumerable<int> answers) {
            GenerateExercive(out int a, out int b, out OP op, out rightAnswer);
            SetText(a, b, op);
            answers = GenerateAnswers(answersCount, rightAnswer);
        }
        
        public void ShowAnswer(int answer, bool isRight) {
            _txtAnswer.text = answer.ToString();
            SetColor(isRight ? _colorRight : _colorWrong);
        }

        private void GenerateExercive(out int a, out int b, out OP op, out int answer) {
            a = 1; b = 1; op = OP.PLUS;
            answer = 2;
            for (int i = 0; i < _ATTEMPTS; ++i) {
                var op1 = _operations.Random();
                var a1 = Random.Range(_minNumber, _maxNumber + 1);
                var min = _minNumber;
                var max = _maxNumber;
                switch (op1) {
                    case OP.PLUS:
                        max = Mathf.Min(max, _maxAnswer - a1);
                        break;
                    case OP.MINUS:
                        max = Mathf.Min(max, a1);
                        break;
                    case OP.MULT:
                        max = Mathf.Min(max, _maxAnswer / a1);
                        break;
                }
                if (min <= max) {
                    a = a1;
                    op = op1;
                    b = Random.Range(min, max + 1);
                    switch (op) {
                        case OP.PLUS:
                            answer = a + b;
                            break;
                        case OP.MINUS:
                            answer = a - b;
                            break;
                        case OP.MULT:
                            answer = a * b;
                            break;
                    }
                    return;
                }
            }
        }

        private IEnumerable<int> GenerateAnswers(int answersCount, int rightAnswer) {
            var max = Mathf.Min(_maxNumber + _maxNumber, _maxAnswer);
            var was = new SortedSet<int> {rightAnswer};
            var rightAnswerPlace = Random.Range(-1, answersCount - 1);
            if (rightAnswerPlace == -1)
                yield return rightAnswer;
            for (int i = 0; i < answersCount - 1; ++i) {
                var r = 0;
                for (int j = 0; j < _ATTEMPTS; ++j) {
                    var r1 = Random.Range(0, max + 1);
                    if (!was.Contains(r1)) {
                        r = r1;
                        break;
                    }
                }

                was.Add(r);
                yield return r;
                if (i == rightAnswerPlace)
                    yield return rightAnswer;
            }
        }

        private void SetText(int a, int b, OP op) {
            var txtOp = ' ';
            switch (op) {
                case OP.PLUS:
                    txtOp = '+';
                    break;
                case OP.MINUS:
                    txtOp = '-';
                    break;
                case OP.MULT:
                    txtOp = '×';
                    break;
            }
            _txtExercive.text = $"{a} {txtOp} {b} =";
            _txtAnswer.text = "";
            SetColor(_colorNeutral);
        }

        private void SetColor(Color color) {
            _txtExercive.color = color;
            _txtAnswer.color = color;
        }
    }
}