import sys
import json
from imitate import imitate

if __name__ == "__main__":
    data = json.load(sys.stdin)
    prompt = data["prompt"]
    image_b64 = data["image_b64"]
    amount = data["amount"]

    results = imitate(prompt, image_b64, amount)
    json.dump(results, sys.stdout)