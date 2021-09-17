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

	[HideInInspector] public KMSelectable[] DigitsKeys = new KMSelectable[10];
	[HideInInspector] public KMSelectable ClearKey;
	[HideInInspector] public KMSelectable SubmitKey;

	public int ExpectedZ { get; private set; }

	private int X;
	private int A;
	private int D;
	private int ExpectedSum;
	private int StaticY;

	private int _z = 0; public int Z { get { return _z; } private set { if (this.Z == value) return; this._z = value; UpdateZDisplay(); } }
	private int _k = -1; public int K { get { return _k; } private set { if (this.K == value) return; this._k = value; UpdateKDisplay(); } }

	private void Start() {
		int[] xDigits = Enumerable.Repeat(0, DIGITS_COUNT).Select(_ => Random.Range(0, 10)).ToArray();
		while (xDigits.Sum() < MIN_DIGIT_SUM) IncreaseRandomDigit(xDigits);
		X = DigitsToNumber(xDigits);
		A = xDigits.Sum() + 1;
		D = Random.Range(0, A);
		Log("X = {0}", X.ToString());
		Log("A = {0}", A.ToString());
		Log("D = {0}", D.ToString());
		ExpectedSum = CalculateExpectedSum(X, D);
		Log("Expected sum = {0}", ExpectedSum.ToString());
		ExpectedZ = ExpectedSum - X;
		if (ExpectedZ < 0) ExpectedZ += MODULER;
		Log("Z = {0}", ExpectedZ.ToString());
		List<KeyComponent> keys = new List<KeyComponent>();
		for (int i = 0; i < 10; i++) {
			int digit = (i + 1) % 10;
			KeyComponent key = CreateKey(new Vector2Int(i % 5, i / 5), digit.ToString(), KeyComponent.DEFAULT_COLOR);
			key.Selectable.OnInteract += () => { OnDigitPressed(digit); return false; };
			DigitsKeys[digit] = key.Selectable;
			keys.Add(key);
		}
		KeyComponent clearKey = CreateKey(new Vector2Int(5, 0), "<", Color.red);
		clearKey.Selectable.OnInteract += () => { OnCancelPressed(); return false; };
		ClearKey = clearKey.Selectable;
		keys.Add(clearKey);
		KeyComponent submitKey = CreateKey(new Vector2Int(5, 1), "S", Color.green);
		submitKey.Selectable.OnInteract += () => { OnSubmitPressed(); return false; };
		SubmitKey = submitKey.Selectable;
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
			StaticY,
			19 * minutesLeft,
			2 * firstDigits,
		}.Sum();
		int newK = D - y;
		newK = (Mathf.Abs(newK * A) + newK) % A;
		if (this.K != newK && !IsSolved) {
			Log("K updated to {0} (Y = {1} = 19 * {2} + 2 * {3})", newK.ToString(), y.ToString(), minutesLeft.ToString(), firstDigits.ToString());
		}
		this.K = newK;
	}

	public override void OnActivate() {
		base.OnActivate();
		XDisplay.text = X.ToString().PadLeft(6, ' ');
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
		this.StaticY = staticYNums.Sum();
		Log("Static Y = {0}", StaticY.ToString());
		Log("13 * {0} batteries", batteries.ToString());
		Log("17 * {0} modules", modules.ToString());
		Log("5 * {0} DVI ports", dviPorts.ToString());
		Log("7 * {0} unlit indicators", unlitIndicators.ToString());
		Log("11 * {0} (last digit of serial number)", lastSerialNumberDigit.ToString());
		Log("3 * {0} starting minutes", startingTimeInMinutes.ToString());
	}

	private void UpdateKDisplay() {
		KDisplay.text = K.ToString().PadLeft(2, ' ');
	}

	private void UpdateZDisplay() {
		ZDisplay.text = Z.ToString().PadLeft(6, ' ');
	}

	public void OnDigitPressed(int digit) {
		if (!IsActive || IsSolved) return;
		this.PlaySound(this.transform, KMSoundOverride.SoundEffect.ButtonPress);
		int newZ = Z * 10 + digit;
		if (newZ >= MODULER) return;
		Z = newZ;
	}

	public void OnCancelPressed() {
		if (!IsActive || IsSolved) return;
		this.PlaySound(this.transform, KMSoundOverride.SoundEffect.ButtonPress);
		Z = 0;
	}

	public void OnSubmitPressed() {
		if (!IsActive || IsSolved) return;
		if (Z == ExpectedZ) {
			this.Log("Module solved");
			this.PlaySound(this.transform, KMSoundOverride.SoundEffect.CorrectChime);
			this.Solve();
		} else {
			this.Log("{0} submitted. {1} expected. Strike!", this.Z.ToString(), this.ExpectedZ.ToString());
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
