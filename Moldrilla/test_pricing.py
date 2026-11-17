import unittest
from server import DEFAULT, calculate, validate_settings, estimate_project


class PricingTests(unittest.TestCase):
    def setUp(self):
        self.settings = dict(DEFAULT, design_hour=100, setup=50, tool_cm3=2,
                             silicone_kg=1000, labor_hour=100, waste_percent=10,
                             margin_percent=40, materials={'Resin': 3, 'Wax': 1, 'Concrete': 1, 'Plaster': 1})

    def test_silicone_units_and_once_only_costs(self):
        q = calculate(self.settings, {'kind':'mold','quantity':2,'design_hours':1,'tool_volume':10,'silicone_volume':100,'labor_hours':1})
        # 150 development + 22 tooling + 242 silicone + 100 labor = 514.
        self.assertEqual(q['cost'], 514)
        self.assertEqual(q['total'], 856.67)

    def test_piece_batch_cost_and_shipping(self):
        q = calculate(self.settings, {'kind':'pieces','quantity':10,'material':'Wax','piece_volume':20,'shipping':40})
        self.assertEqual(q['cost'], 270)
        self.assertEqual(q['total'], 490)

    def test_missing_rates_block_estimates(self):
        with self.assertRaises(ValueError):
            calculate(DEFAULT, {'kind':'design'})

    def test_invalid_margin_and_quantity(self):
        with self.assertRaises(ValueError):
            validate_settings(dict(self.settings, margin_percent=100))
        with self.assertRaises(ValueError):
            calculate(self.settings, {'kind':'mold','quantity':1.5})

    def test_minimum_and_design_ignores_production(self):
        q = calculate(dict(self.settings, minimum=500), {'kind':'design','quantity':20,'silicone_volume':200})
        self.assertEqual(q['total'], 500)
        self.assertEqual(q['quantity'], 1)

    def test_gallery_estimate_uses_private_assumptions(self):
        brief = {'product': 'candle', 'kind': 'mold', 'starting': 'idea',
                 'material': 'Wax', 'quantity': 1, 'length': 60, 'width': 60, 'height': 60,
                 'finish': 'Raw / unpainted'}
        result = estimate_project(self.settings, brief)
        self.assertGreater(result['total'], 0)
        self.assertTrue(result['preliminary'])
        self.assertNotIn('cost', result)

    def test_uncertain_geometry_and_missing_rates_require_review(self):
        brief = {'product': 'candle', 'kind': 'mold', 'starting': 'idea',
                 'material': 'Wax', 'quantity': 1, 'length': 60, 'width': 60, 'height': 60}
        self.assertIsNone(estimate_project(DEFAULT, brief)['total'])
        self.assertIsNone(estimate_project(self.settings, dict(brief, unknown_size=True))['total'])
        self.assertIsNone(estimate_project(self.settings, dict(brief, product='custom'))['total'])

    def test_wax_print_is_distinct_and_requires_print_review(self):
        quote = estimate_project(self.settings, {'kind': 'print', 'product': 'ring', 'material': 'Castable wax'})
        self.assertIsNone(quote['total'])
        self.assertIn('wax pattern', quote['reason'])


if __name__ == '__main__':
    unittest.main()
