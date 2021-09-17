using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using KeepCoding;

public class DigitSumTPScript : TPScript<DigitSumModule> {
	/*
	"!{0} 123456" - submit an answer
	*/

	public override IEnumerator Process(string command) {
		command = command.Trim().ToLower();
		if (command.StartsWith("submit ")) command = command.Skip(7).Join("").Trim();
		command = command.Split(' ').Join("");
		if (!Regex.IsMatch(command, @"^\d{1,6}$")) yield break;
		yield return null;
		if (Module.Z > 0) yield return new[] { Module.ClearKey };
		yield return command.Select(c => c - '0').Select(d => Module.DigitsKeys[d]).ToArray();
		yield return new[] { Module.SubmitKey };
	}

	public override IEnumerator ForceSolve() {
		yield return new WaitForSeconds(.2f);
		if (Module.Z > 0) {
			Module.OnCancelPressed();
			yield return new WaitForSeconds(.2f);
		}
		foreach (int digit in Module.ExpectedZ.ToString().Select(c => c - '0')) {
			Module.OnDigitPressed(digit);
			yield return new WaitForSeconds(.2f);
		}
		Module.OnSubmitPressed();
	}
}
