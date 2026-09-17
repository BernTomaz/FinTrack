from pathlib import Path

from PIL import Image


ASSETS = Path("docs/assets")
FRAMES = ASSETS / "gif-frames"
GIFS = {
    "login": "fintrack-login.gif",
    "dashboard": "fintrack-dashboard.gif",
    "accounts": "fintrack-accounts.gif",
}


def load_frame(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGB")
    width, height = image.size
    if width > 900:
        height = round(height * 900 / width)
        image = image.resize((900, height), Image.Resampling.LANCZOS)
    return image


for group, output in GIFS.items():
    files = sorted((FRAMES / group).glob("*.png"))
    if not files:
        raise SystemExit(f"No frames found for {group}")

    frames = [load_frame(file) for file in files]
    frames[0].save(
        ASSETS / output,
        save_all=True,
        append_images=frames[1:],
        duration=1000,
        loop=0,
        optimize=True,
    )
