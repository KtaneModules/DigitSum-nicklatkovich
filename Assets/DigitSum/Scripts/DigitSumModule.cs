using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using KeepCoding;

public class DigitSumModule : ModuleScript {
	private static readonly Vector2 KEYS_OFFSET = new Vector2(.025f, -.025f);
	private const int DIGITS_COUNT = 6;
	private const int MIN_DIGIT_SUM = 10;
	private const int MODULER = 1000000;

	public Transform KeyboardContainer;
	public TextMesh KDisplay;
	public TextMesh XDisplay;
	public TextMesh ZDisplay;
	public KMSelectable Selectable;
	public KMBombInfo BombInfo;
	public KeyComponent KeyPrefab;

	private int x;
	private int a;
	private int d;
	private int expectedSum;
	private int expectedZ;
	private int staticY;

	private int _z = 0; public int z { get { return _z; } private set { if (this.z == value) return; this._z = value; UpdateZDisplay(); } }
	private int _k = -1; public int k { get { return _k; } private set { if (this.k == value) return; this._k = value; UpdateKDisplay(); } }

	private void Start() {
		int[] xDigits = Enumerable.Repeat(0, DIGITS_COUNT).Select(_ => Random.Range(0, 10)).ToArray();
		while (xDigits.Sum() < MIN_DIGIT_SUM) IncreaseRandomDigit(xDigits);
		x = DigitsToNumber(xDigits);
		a = xDigits.Sum() + 1;
		d = Random.Range(0, a);
		Log("X = {0}", new[] { x });
		Log("A = {0}", new[] { a });
		Log("D = {0}", new[] { d });
		expectedSum = CalculateExpectedSum(x, d);
		Log("Expected sum = {0}", new[] { expectedSum });
		expectedZ = expectedSum - x;
		if (expectedZ < 0) expectedZ += MODULER;
		Log("Z = {0}", new[] { expectedZ });
		List<KeyComponent> keys = new List<KeyComponent>();
		for (int i = 0; i < 10; i++) {
			int digit = (i + 1) % 10;
			KeyComponent key = CreateKey(new Vector2Int(i % 5, i / 5), digit.ToString(), KeyComponent.DEFAULT_COLOR);
			key.Selectable.OnInteract += () => { OnDigitPressed(digit); return false; };
			keys.Add(key);
		}
		KeyComponent clearKey = CreateKey(new Vector2Int(5, 0), "<", Color.red);
		clearKey.Selectable.OnInteract += () => { OnCancelPressed(); return false; };
		keys.Add(clearKey);
		KeyComponent submitKey = CreateKey(new Vector2Int(5, 1), "S", Color.green);
		submitKey.Selectable.OnInteract += () => { OnSubmitPressed(); return false; };
		keys.Add(submitKey);
		Selectable.Children = keys.Select(k => k.Selectable).ToArray();
		Selectable.UpdateChildren();
	}

	private void Update() {
		if (!IsActive) return;
		int minutesLeft = Mathf.FloorToInt(BombInfo.GetTime()) / 60;
		int[] codes = BombInfo.GetTwoFactorCodes().ToArray();
		int firstDigits = codes.Select(code => int.Parse(code.ToString()[0].ToString())).Sum();
		int y = new[] {
			staticY,
			19 * minutesLeft,
			2 * firstDigits,
		}.Sum();
		int newK = d - y;
		newK = (Mathf.Abs(newK * a) + newK) % a;
		if (this.k != newK && !IsSolved) {
			Log("K updated to {0} (Y = {1} = 19 * {2} + 2 * {3})", new string[] { newK.ToString(), y.ToString(), minutesLeft.ToString(), firstDigits.ToString() });
		}
		this.k = newK;
	}

	public override void OnActivate() {
		base.OnActivate();
		XDisplay.text = x.ToString().PadLeft(6, ' ');
		UpdateZDisplay();
		int batteries = BombInfo.GetBatteryCount();
		int modules = BombInfo.GetModuleIDs().Count;
		int dviPorts = BombInfo.GetPortCount(Port.DVI);
		int unlitIndicators = BombInfo.GetOffIndicators().Count();
		int lastSerialNumberDigit = BombInfo.GetSerialNumberNumbers().Last();
		int startingTimeInMinutes = Mathf.FloorToInt(BombInfo.GetTime()) / 60;
		int[] staticYNums = new[] {
			13 * batteries,
			17 * modules,
			5 * dviPorts,
			7 * unlitIndicators,
			11 * lastSerialNumberDigit,
			3 * startingTimeInMinutes,
		};
		this.staticY = staticYNums.Sum();
		Log("Static Y = {0}", new[] { staticY });
		Log("13 * {0} batteries", new[] { batteries });
		Log("17 * {0} modules", new[] { modules });
		Log("5 * {0} DVI ports", new[] { dviPorts });
		Log("7 * {0} unlit indicators", new[] { unlitIndicators });
		Log("11 * {0} (last digit of serial number)", new[] { lastSerialNumberDigit });
		Log("3 * {0} starting minutes", new[] { startingTimeInMinutes });
	}

	private void UpdateKDisplay() {
		KDisplay.text = k.ToString().PadLeft(2, ' ');
	}

	private void UpdateZDisplay() {
		ZDisplay.text = z.ToString().PadLeft(6, ' ');
	}

	private void OnDigitPressed(int digit) {
		if (!IsActive || IsSolved) return;
		z = (z * 10 + digit) % MODULER;
	}

	private void OnCancelPressed() {
		if (!IsActive || IsSolved) return;
		z = 0;
	}

	private void OnSubmitPressed() {
		if (!IsActive || IsSolved) return;
		if (z == expectedZ) {
			this.Log("Module solved");
			this.Solve();
		} else {
			this.Log(string.Format("{0} submitted. {1} expected. Strike!", this.z, this.expectedZ));
			this.Log("{0} submitted. {1} expected. Strike!", new string[] { this.z.ToString(), this.expectedZ.ToString() });
			this.Strike();
		}
	}

	private KeyComponent CreateKey(Vector2Int pos, string label, Color color) {
		KeyComponent key = Instantiate(KeyPrefab);
		key.transform.parent = KeyboardContainer;
		key.transform.localPosition = new Vector3(pos.x * KEYS_OFFSET.x, 0, pos.y * KEYS_OFFSET.y);
		key.transform.localScale = Vector3.one;
		key.transform.localRotation = Quaternion.identity;
		key.Selectable.Parent = Selectable;
		key.label = label.ToString();
		key.color = color;
		return key;
	}

	private static void IncreaseRandomDigit(int[] digits) {
		digits[Enumerable.Range(0, DIGITS_COUNT).Where(i => digits[i] < 9).PickRandom()] += 1;
	}

	private static int DigitsToNumber(int[] digits) {
		int denom = 1;
		int res = 0;
		foreach (int digit in digits) {
			res += denom * digit;
			denom *= 10;
		}
		return res;
	}

	private static int[] NumberToDigits(int num) {
		int[] res = new int[DIGITS_COUNT];
		for (int i = 0; i < DIGITS_COUNT; i++) {
			res[i] = num % 10;
			num /= 10;
		}
		return res;
	}

	private static int CalculateExpectedSum(int x, int d) {
		int denom = 1;
		for (int i = 0; i < DIGITS_COUNT; i++) {
			if (NumberToDigits(x).Sum() <= d) break;
			int digit = x / denom % 10;
			if (digit > 0) x += denom * (10 - digit);
			denom *= 10;
		}
		while (NumberToDigits(x).Sum() < d) x += 1;
		return x % MODULER;
	}
}
