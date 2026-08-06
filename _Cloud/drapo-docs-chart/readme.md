# Helm Chart for Drapo Docs

Drapo documentation app.

Application source code: https://github.com/spadrapo/docs

Source Image: https://github.com/spadrapo/docs/pkgs/container/docs

## Run locally

```sh
docker run --rm -p 5000:5000 --network host ghcr.io/spadrapo/docs:latest
# Access at http://localhost:5000
```

## Chart Installation

1. Create namespace:

```sh
kubectl create ns drapo-docs
```

2. Configure `values.yaml`.


3. Verify chart output (dry run):

```bash
helm template --namespace drapo-docs drapo-docs-chart ./
```

4. Install chart:

```bash
helm upgrade --install --namespace drapo-docs drapo-docs-chart ./
```

5. Uninstall chart:

```bash
helm uninstall --namespace drapo-docs drapo-docs-chart
```
