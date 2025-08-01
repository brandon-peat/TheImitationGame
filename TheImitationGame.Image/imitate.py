import requests
import base64
from PIL import Image
from io import BytesIO
from postprocess import batch_post_process

def imitate(prompt: str, image_b64: str, amount: int) -> list[str]:
    raw_images = generate_images_a1111(prompt, image_b64, amount)
    post_processed_images = batch_post_process(raw_images)
    return post_processed_images

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
        "steps": 20,
        "denoising_strength": 0.75,
        "sampler_name": "Euler a",
        "batch_size": amount,
        "width": 512,
        "height": 512,
        "alwayson_scripts": {
            "controlnet": {
                "args": [
                    {
                        "input_image": image_b64,
                        "module": "canny",
                        "model": "control_v11p_sd15_canny [d14c016b]",
                        "weight": 1.0,
                        "resize_mode": "Scale to Fit (Inner Fit)",
                        "control_mode": "Balanced",
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