"""Export the Science API from the running development host, retaining referenced schemas."""
import argparse
import json
from pathlib import Path
from urllib.request import urlopen


def export(base_url, destination):
    with urlopen(base_url.rstrip("/") + "/swagger/v1/swagger.json", timeout=15) as response:
        spec = json.load(response)
    spec["info"]["title"] = "Science and Technology API"
    spec["paths"] = {k: v for k, v in spec["paths"].items() if "/science-technology" in k}
    if "/api/science-technology/technologies/{slug}" not in spec["paths"]:
        raise ValueError("The host does not expose the Science and Technology API")
    schemas = spec["components"]["schemas"]
    selected = {}

    def visit(value):
        if isinstance(value, list):
            for item in value:
                visit(item)
        elif isinstance(value, dict):
            reference = value.get("$ref", "")
            if reference.startswith("#/components/schemas/"):
                key = reference.split("/")[-1]
                if key not in selected:
                    selected[key] = schemas[key]
                    visit(selected[key])
            for item in value.values():
                visit(item)

    visit(spec["paths"])
    spec["components"]["schemas"] = dict(sorted(selected.items()))
    destination.write_text(json.dumps(spec, indent=2) + chr(10), encoding="utf-8")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default="http://127.0.0.1:5086")
    args = parser.parse_args()
    export(args.base_url, Path(__file__).resolve().parents[1] / "postman/specs/science-technology.openapi.json")
