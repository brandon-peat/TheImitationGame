from PIL import Image, ImageOps
import base64
from io import BytesIO

def base64_to_pil_image(b64_string):
    image_data = base64.b64decode(b64_string)
    return Image.open(BytesIO(image_data))

def pil_image_to_base64(img):
    buffered = BytesIO()
    img.save(buffered, format="PNG")
    return base64.b64encode(buffered.getvalue()).decode()

def batch_post_process(images_b64, downscale_factor=2, posterize_bits=3):
    processed_images_b64 = []

    for b64_image in images_b64:
        img = base64_to_pil_image(b64_image).convert("RGB")

        # Posterize
        img = ImageOps.posterize(img, posterize_bits)

        # Pixelate (downscale and upscale)
        w, h = img.size
        img = img.resize((w // downscale_factor, h // downscale_factor), Image.NEAREST)
        img = img.resize((w, h), Image.NEAREST)

        # Convert back to base64
        processed_b64 = pil_image_to_base64(img)
        processed_images_b64.append(processed_b64)

    return processed_images_b64