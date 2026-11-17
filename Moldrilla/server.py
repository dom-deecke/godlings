"""Moldrilla local preview. Run: python3 server.py"""
import json
import math
import os
import tempfile
import base64
import uuid
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

ROOT = Path(__file__).parent
DATA = ROOT / '.data' / 'pricing.json'
DEFAULT = {'currency': 'MXN', 'design_hour': None, 'setup': None,
           'tool_cm3': None, 'silicone_kg': None, 'silicone_density': 1.1,
           'labor_hour': None, 'waste_percent': 10, 'margin_percent': 40,
           'minimum': 0, 'simple_hours': 1, 'medium_hours': 3, 'complex_hours': 6,
           'mold_labor_hours': .75, 'piece_labor_hours': .1, 'mold_wall_mm': 10,
           'master_fill_percent': 20,
           'materials': {'Resin': None, 'Wax': None, 'Concrete': None, 'Plaster': None}}


def number(value, label, maximum=1e9):
    try:
        result = float(value)
    except (TypeError, ValueError):
        raise ValueError(f'{label}: enter a number.')
    if not math.isfinite(result) or result < 0 or result > maximum:
        raise ValueError(f'{label}: enter a valid nonnegative amount.')
    return result


def validate_settings(data):
    result = {'currency': 'MXN'}
    for key in DEFAULT:
        if key in ('currency', 'materials'):
            continue
        value = data.get(key)
        result[key] = None if value is None or value == '' else number(value, key)
    if result['margin_percent'] is None or result['margin_percent'] >= 100:
        raise ValueError('Gross margin must be between 0 and less than 100%.')
    if not result['silicone_density']:
        raise ValueError('Silicone density must be greater than zero.')
    for key in ('waste_percent', 'minimum', 'simple_hours', 'medium_hours', 'complex_hours',
                'mold_labor_hours', 'piece_labor_hours', 'mold_wall_mm', 'master_fill_percent'):
        if result[key] is None:
            raise ValueError(f'{key} cannot be blank.')
    result['materials'] = {name: None if data.get('materials', {}).get(name) in (None, '') else
                           number(data['materials'][name], name) for name in DEFAULT['materials']}
    return result


def load_settings():
    return dict(DEFAULT, **json.loads(DATA.read_text())) if DATA.exists() else DEFAULT.copy()


PRODUCTS = {'figurine': (.34, 'complex'), 'candle': (.6, 'simple'),
            'planter': (.22, 'medium'), 'dice': (.5, 'medium'),
            'chocolate': (.72, 'simple'), 'keychain': (.7, 'simple'), 'ring': (.12, 'complex')}


def estimate_project(settings, brief):
    """A preliminary approximation, never a replacement for geometry review."""
    pending = lambda reason: {'total': None, 'reason': reason}
    if brief.get('kind') == 'print':
        return pending('We’ll check printability and the printing material before quoting your physical print or investment-casting wax pattern.')
    if brief.get('product') not in PRODUCTS or brief.get('unknown_size'):
        return pending('Your original shape or dimensions need a closer look.')
    if brief.get('finish') not in ('Raw / unpainted', None):
        return pending('We’ll confirm your finishing requirements before setting a price.')
    fraction, complexity = PRODUCTS[brief['product']]
    dims = [number(brief.get(k), k, 1000) for k in ('length', 'width', 'height')]
    if min(dims) < 1:
        raise ValueError('Dimensions must be at least 1 mm.')
    quantity = number(brief.get('quantity', 1), 'Quantity', 100000)
    if quantity < 1 or quantity != int(quantity):
        raise ValueError('Quantity must be a positive whole number.')
    volume = math.prod(dims) / 1000 * fraction
    wall = settings['mold_wall_mm']
    silicone = math.prod(d + 2 * wall for d in dims) / 1000 - volume
    kind = brief.get('kind')
    design_hours = 0 if brief.get('starting') == 'file' else settings[complexity + '_hours']
    if brief.get('starting') == 'object':
        return pending('We’ll review your physical object and how to reproduce it.')
    if brief.get('material') in ('Chocolate', 'Ice', 'Soap', 'Help me choose'):
        return pending('We’ll confirm the appropriate material and process for your project.')
    if kind == 'pieces' and quantity > 20:
        return pending('Your batch needs a production and mold-life review.')
    request = {'kind': kind, 'quantity': quantity, 'design_hours': design_hours,
               'tool_volume': volume * settings['master_fill_percent'] / 100,
               'silicone_volume': silicone, 'mold_count': 1,
               'labor_hours': settings['mold_labor_hours'] * (quantity if kind == 'mold' else 1) +
                              (settings['piece_labor_hours'] * quantity if kind == 'pieces' else 0),
               'material': brief.get('material'), 'piece_volume': volume}
    try:
        quote = calculate(settings, request)
        return {'total': quote['total'], 'currency': quote['currency'], 'preliminary': True}
    except ValueError as exc:
        if 'Admin pricing needed' in str(exc):
            return pending('Your project needs a tailored quote from the workshop.')
        raise


def calculate(settings, request):
    kind = request.get('kind')
    if kind not in ('design', 'mold', 'pieces'):
        raise ValueError('Select a deliverable.')
    quantity = number(request.get('quantity', 1), 'Quantity', 100000)
    if quantity < 1 or quantity != int(quantity):
        raise ValueError('Quantity must be a whole number of at least one.')
    if kind == 'design':
        quantity = 1
    lines = []

    def add(label, amount, rate_key=None, direct_rate=None):
        amount = number(amount, label)
        if not amount:
            return
        rate = direct_rate if rate_key is None else settings.get(rate_key)
        if rate is None:
            raise ValueError(f'Admin pricing needed: {label}.')
        lines.append({'label': label, 'cost': round(amount * number(rate, label), 4)})

    add('Design', request.get('design_hours', 0), 'design_hour')
    add('Project setup', 1, 'setup')
    waste = 1 + settings['waste_percent'] / 100
    if kind != 'design':
        # Tooling is a project-level amount: printed masters and mold-making boxes.
        add('Master / tooling material', number(request.get('tool_volume', 0), 'Tooling volume') * waste, 'tool_cm3')
        molds = quantity if kind == 'mold' else number(request.get('mold_count', 1), 'Mold count', 100000)
        if molds < 1 or molds != int(molds):
            raise ValueError('Mold count must be a whole number of at least one.')
        silicone_cm3 = number(request.get('silicone_volume', 0), 'Silicone volume')
        add('Silicone', silicone_cm3 * molds * settings['silicone_density'] / 1000 * waste, 'silicone_kg')
        add('Production labor', request.get('labor_hours', 0), 'labor_hour')
        if kind == 'pieces':
            material = request.get('material')
            if material not in settings['materials']:
                raise ValueError('Choose a finished-piece material.')
            volume = number(request.get('piece_volume', 0), 'Piece volume')
            if volume <= 0:
                raise ValueError('Finished-piece volume must be greater than zero.')
            add('Casting material', volume * quantity * waste, direct_rate=settings['materials'][material])
    add('Finishing / packaging cost', request.get('extras', 0), direct_rate=1)
    cost = sum(line['cost'] for line in lines)
    base = max(settings['minimum'], cost / (1 - settings['margin_percent'] / 100))
    shipping = number(request.get('shipping', 0), 'Shipping')
    return {'currency': settings['currency'], 'total': round(base + shipping, 2),
            'subtotal': round(base, 2), 'shipping': shipping, 'unit': round(base / quantity, 2),
            'quantity': int(quantity), 'lines': lines, 'cost': round(cost, 2),
            'note': 'Estimate before tax; subject to design and production review.'}


class Handler(BaseHTTPRequestHandler):
    def send_json(self, data, status=200):
        payload = json.dumps(data).encode()
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Cache-Control', 'no-store')
        self.end_headers()
        self.wfile.write(payload)

    def do_GET(self):
        path = self.path.split('?')[0]
        if path == '/api/pricing':
            return self.send_json(load_settings())
        projects = ROOT / '.data' / 'projects'
        if path == '/api/projects':
            return self.send_json([json.loads(p.read_text()) for p in sorted(projects.glob('*.json'), reverse=True)])
        if path.startswith('/api/projects/') and path.endswith('/attachment'):
            project_id = path.split('/')[3]
            if not project_id.startswith('MLD-') or not project_id[4:].isalnum():
                return self.send_json({'error': 'Not found.'}, 404)
            meta = projects / (project_id + '.json')
            if not meta.exists():
                return self.send_json({'error': 'Not found.'}, 404)
            project = json.loads(meta.read_text())
            attachment = projects / (project_id + '.upload')
            if not attachment.exists():
                return self.send_json({'error': 'Not found.'}, 404)
            self.send_response(200)
            self.send_header('Content-Type', 'application/octet-stream')
            self.send_header('Content-Disposition', "attachment; filename*=UTF-8''" + __import__('urllib.parse', fromlist=['quote']).quote(project['attachment']))
            self.end_headers()
            self.wfile.write(attachment.read_bytes())
            return
        filename = {'/': 'index.html', '/admin': 'index.html', '/app.js': 'app.js', '/renderer.js': 'renderer.js', '/style.css': 'style.css'}.get(path)
        if not filename:
            self.send_error(404)
            return
        payload = (ROOT / filename).read_bytes()
        if filename == 'index.html':
            # Keep the localhost preview self-contained so a partial asset load
            # cannot leave the page without styles or form behavior.
            page = payload.decode('utf-8')
            page = page.replace('<link rel="stylesheet" href="/style.css">',
                                '<style>' + (ROOT / 'style.css').read_text() + '</style>')
            page = page.replace('<script src="/app.js" defer></script>', '')
            page = page.replace('<script src="/renderer.js"></script>', '')
            page = page.replace('</body>', '<script>' + (ROOT / 'renderer.js').read_text() + '</script><script>' + (ROOT / 'i18n.js').read_text() + '</script><script>' + (ROOT / 'app.js').read_text() + '</script></body>')
            payload = page.encode('utf-8')
        self.send_response(200)
        self.send_header('Content-Type', {'html': 'text/html', 'js': 'text/javascript', 'css': 'text/css'}[filename.split('.')[-1]] + '; charset=utf-8')
        self.send_header('Cache-Control', 'no-store')
        self.send_header('Content-Length', str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def do_POST(self):
        # This development app never listens outside loopback; reject cross-site writes.
        if self.headers.get('Origin') not in (None, 'http://127.0.0.1:4177', 'http://localhost:4177'):
            return self.send_json({'error': 'Origin not allowed.'}, 403)
        try:
            length = int(self.headers.get('Content-Length', '0'))
            if length > 12000000:
                raise ValueError('Request too large.')
            data = json.loads(self.rfile.read(length))
            if self.path == '/api/estimate':
                return self.send_json(estimate_project(load_settings(), data))
            if self.path == '/api/projects':
                if not str(data.get('name', '')).strip() or '@' not in str(data.get('email', '')):
                    raise ValueError('Please provide your name and email address.')
                if data.get('kind') not in ('design', 'mold', 'print', 'pieces'):
                    raise ValueError('Choose a deliverable.')
                project_id = 'MLD-' + uuid.uuid4().hex[:12].upper()
                directory = ROOT / '.data' / 'projects'
                directory.mkdir(parents=True, exist_ok=True)
                project = {key: str(data.get(key, ''))[:5000] for key in
                           ('product', 'kind', 'starting', 'length', 'width', 'height', 'material',
                            'quantity', 'finish', 'description', 'name', 'email', 'postal', 'date')}
                project.update(id=project_id, created=datetime.now(timezone.utc).isoformat(),
                               unknown_size=bool(data.get('unknown_size')), attachment=None)
                reference = data.get('reference')
                if isinstance(reference, dict):
                    name = Path(str(reference.get('name', ''))).name
                    if Path(name).suffix.lower() not in ('.png', '.jpg', '.jpeg', '.webp', '.pdf', '.stl', '.obj'):
                        raise ValueError('Unsupported reference file type.')
                    content = base64.b64decode(reference.get('data', ''), validate=True)
                    if len(content) > 8 * 1024 * 1024:
                        raise ValueError('Reference files must be 8 MB or smaller.')
                    (directory / (project_id + '.upload')).write_bytes(content)
                    project['attachment'] = name
                (directory / (project_id + '.json')).write_text(json.dumps(project, indent=2))
                return self.send_json({'saved': True, 'id': project_id}, 201)
            if self.path == '/api/pricing':
                settings = validate_settings(data)
                DATA.parent.mkdir(exist_ok=True)
                fd, tmp = tempfile.mkstemp(dir=DATA.parent)
                with os.fdopen(fd, 'w') as handle:
                    json.dump(settings, handle, indent=2)
                os.replace(tmp, DATA)
                return self.send_json({'saved': True, 'settings': settings})
            if self.path == '/api/quote':
                return self.send_json(calculate(load_settings(), data))
            return self.send_json({'error': 'Not found.'}, 404)
        except (ValueError, TypeError, KeyError) as exc:
            return self.send_json({'error': str(exc)}, 400)


if __name__ == '__main__':
    print('Moldrilla: http://127.0.0.1:4177\nAdmin: http://127.0.0.1:4177/admin', flush=True)
    ThreadingHTTPServer(('127.0.0.1', 4177), Handler).serve_forever()
