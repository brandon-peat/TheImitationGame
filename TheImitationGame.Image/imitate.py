import requests
import base64
from PIL import Image
from io import BytesIO

def imitate(prompt: str, image_b64: str, amount: int) -> list[str]:
    images = generate_images_a1111(prompt, image_b64, amount)
    return images

def image_to_base64(path):
    with open(path, "rb") as f:
        return base64.b64encode(f.read()).decode()

def base64_to_image(b64_data, output_path):
    image = Image.open(BytesIO(base64.b64decode(b64_data)))
    image.save(output_path)

def generate_images_a1111(prompt, image_b64, amount):
    url = "http://127.0.0.1:7860/sdapi/v1/img2img"

    payload = {
        "prompt": prompt,
        "init_images": [image_b64],
        "steps": 15,
        "denoising_strength": 0.3,
        "sampler_name": "Euler a",
        "batch_size": amount,
        "width": 512,
        "height": 512,
        "alwayson_scripts": {
            "controlnet": {
                "args": [
                    {
                        "image": image_b64,
                        "module": "canny",
                        "model": "control_sd15_canny [fef5e48e]",
                        "weight": 1.35,
                        "resize_mode": "Resize and Fill",
                        "control_mode": "ControlNet is more important",
                        "guidance_start": 0.0,
                        "guidance_end": 1.0,
                        "pixel_perfect": True
                    }
                ]
            }
        }
    }

    response = requests.post(url, json=payload)
    response.raise_for_status()
    result = response.json()

    return result["images"]