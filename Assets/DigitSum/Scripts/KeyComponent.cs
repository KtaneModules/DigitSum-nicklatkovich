using UnityEngine;

public class KeyComponent : MonoBehaviour {
	public static readonly Color DEFAULT_COLOR = new Color32(0xaa, 0xaa, 0xaa, 0xff);

	public KMSelectable Selectable;
	public TextMesh Text;

	private string _label; public string label { get { return _label; } set { if (_label == value) return; _label = value; UpdateLabel(); } }
	private Color _color = DEFAULT_COLOR; public Color color { get { return _color; } set { if (_color == value) return; _color = value; UpdateColor(); } }

	private void Start() {
		UpdateLabel();
		UpdateColor();
	}

	private void UpdateLabel() {
		Text.text = _label;
	}

	private void UpdateColor() {
		Text.color = _color;
	}
}
