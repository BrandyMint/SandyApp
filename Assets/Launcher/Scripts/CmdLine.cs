using System;
using System.Collections.Generic;
using Launcher.Scripts;
using UnityEngine;

public static partial class Prefs {
    public static readonly CmdLine CmdLine = new CmdLine();
}

namespace Launcher.Scripts {
    public class CmdLine {
        public string Record { get; private set; }

        public CmdLine() {
            var args = new Queue<string>(Environment.GetCommandLineArgs());
            args.Dequeue();
            while (args.Count > 0) {
                var param = args.Dequeue();
                switch (param) {
                    case "-r":
                    case "--record":
                        ParseRecordFile(param, args);                        
                        break;
                }
            }
        }

        private void ParseRecordFile(string param, Queue<string> args) {
            if (NotEnoughArgs(param, args))
                return;
            
            Record = args.Dequeue();
            PrintAccepted(param, Record);
        }
        
        private bool NotEnoughArgs(string param, Queue<string> args, int mustBeCount = 1) {
            if (args.Count < mustBeCount) {
                PrintError($"In param {param} not enough args!");
                return true;
            }
            return false;
        }

        private void PrintError(string message) {
            Debug.LogWarning(message);
        }

        private void PrintAccepted(string param, params string[] args) {
            Debug.Log($"Accept {param} {string.Join(" ", args)}");
        }
    }
}