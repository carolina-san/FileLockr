const express = require('express');
const router = express.Router();

/**
 * @swagger
 * /usuarios:
 *   get:
 *     summary: Obtiene una lista de usuarios.
 *     responses:
 *       200:
 *         description: Lista de usuarios.
 *         content:
 *           application/json:
 *             schema:
 *               type: array
 *               items:
 *                 type: object
 *                 properties:
 *                   id:
 *                     type: integer
 *                     description: El ID del usuario.
 *                   nombre:
 *                     type: string
 *                     description: El nombre del usuario.
 */

router.get('/usuarios', (req, res) => {
  res.json([{ id: 1, nombre: 'Juan' }, { id: 2, nombre: 'María' }]);
});

module.exports = router;
