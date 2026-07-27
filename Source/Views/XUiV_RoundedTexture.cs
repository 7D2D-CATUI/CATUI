using UnityEngine;

public class XUiV_RoundedTexture : XUiV_Texture
{
	private int cornerRadius;
	private Color fillColor;
	private Color borderColor;
	private int borderWidth;

	[XuiXmlAttribute("corner_radius", false)]
	public int CornerRadius
	{
		get { return cornerRadius; }
		set
		{
			cornerRadius = Mathf.Clamp(value, 0, 100);
			GenerateRoundedTexture();
		}
	}

	[XuiXmlAttribute("fill_color", false)]
	public Color FillColor
	{
		get { return fillColor; }
		set
		{
			fillColor = value;
			GenerateRoundedTexture();
		}
	}

	[XuiXmlAttribute("border_color", false)]
	public Color BorderColor
	{
		get { return borderColor; }
		set
		{
			borderColor = value;
			GenerateRoundedTexture();
		}
	}

	[XuiXmlAttribute("border_width", false)]
	public int BorderWidth
	{
		get { return borderWidth; }
		set
		{
			borderWidth = Mathf.Clamp(value, 0, 10);
			GenerateRoundedTexture();
		}
	}

	public XUiV_RoundedTexture(XUi _xui, string _id)
		: base(_xui, _id)
	{
		cornerRadius = 8;
		fillColor = new Color(0.2f, 0.2f, 0.2f, 1f);
		borderColor = new Color(0.6f, 0.6f, 0.6f, 1f);
		borderWidth = 0;
	}

	public override void InitView()
	{
		base.InitView();
		GenerateRoundedTexture();
	}

	public override void updateData()
	{
		base.updateData();
		if (texture == null && cornerRadius > 0)
		{
			GenerateRoundedTexture();
		}
	}

	private void GenerateRoundedTexture()
	{
		if (size.x <= 0 || size.y <= 0)
			return;

		int width = size.x;
		int height = size.y;
		int radius = Mathf.Min(cornerRadius, width / 2, height / 2);

		const int upscale = 2;
		int texWidth = width * upscale;
		int texHeight = height * upscale;
		int texRadius = radius * upscale;

		Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
		tex.wrapMode = TextureWrapMode.Clamp;
		tex.filterMode = FilterMode.Bilinear;

		Color[] pixels = new Color[texWidth * texHeight];
		for (int y = 0; y < texHeight; y++)
		{
			for (int x = 0; x < texWidth; x++)
			{
				pixels[y * texWidth + x] = CreateRoundedPixel(x, y, texWidth, texHeight, texRadius);
			}
		}

		tex.SetPixels(pixels);
		tex.Apply();

		Texture = tex;
	}

	private Color CreateRoundedPixel(int x, int y, int width, int height, int radius)
	{
		float alpha = 1f;

		if (x <= radius && y <= radius)
		{
			float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
			if (dist > radius)
				alpha = 0f;
			else if (borderWidth > 0 && dist > radius - borderWidth)
				return borderColor;
		}
		else if (x >= width - radius && y <= radius)
		{
			float dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius));
			if (dist > radius)
				alpha = 0f;
			else if (borderWidth > 0 && dist > radius - borderWidth)
				return borderColor;
		}
		else if (x <= radius && y >= height - radius)
		{
			float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1));
			if (dist > radius)
				alpha = 0f;
			else if (borderWidth > 0 && dist > radius - borderWidth)
				return borderColor;
		}
		else if (x >= width - radius && y >= height - radius)
		{
			float dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1));
			if (dist > radius)
				alpha = 0f;
			else if (borderWidth > 0 && dist > radius - borderWidth)
				return borderColor;
		}
		else if (borderWidth > 0)
		{
			if (x < borderWidth || x >= width - borderWidth || y < borderWidth || y >= height - borderWidth)
				return borderColor;
		}

		return new Color(fillColor.r, fillColor.g, fillColor.b, alpha);
	}
}