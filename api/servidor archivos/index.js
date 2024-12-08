const express = require('express');
const swaggerUi = require('swagger-ui-express');
const swaggerJsdoc = require('swagger-jsdoc');
const multer = require('multer');
const path = require('path');
const morgan = require('morgan');
const fs = require('fs');
const jwt = require('jsonwebtoken');

const app = express();
// Configuración de Swagger
const swaggerOptions = {
  definition: {
    openapi: '3.0.0',
    info: {
      title: 'API Documentada con Swagger',
      version: '1.0.0',
    },
  },
  apis: ['./routes/*.js'],
};

const swaggerDocs = swaggerJsdoc(swaggerOptions);
app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(swaggerDocs));
app.use(morgan('dev'));

// Configuración de Multer
const storage = multer.diskStorage({
  destination: (req, file, cb) => {
    cb(null, 'uploads/');
  },
  filename: (req, file, cb) => {
    cb(null, `${file.originalname}`);
  },
});

const upload = multer({ storage });

// Clave secreta para verificar JWT (debe coincidir con la usada para firmar los tokens en tu otro servidor)
const SECRET_KEY = 'aD!3f$9kL@2mQw6v#8xRp1$T2';

// Middleware para verificar el token JWT
const verifyToken = (req, res, next) => {
  const authHeader = req.headers['authorization'];
  console.log('Cabeceras del request:', req.headers);
  if (!authHeader) {
    return res.status(403).json({ message: 'Token requerido' });
  }

  

  const token = authHeader.split(' ')[1];
  console.log('Token extraído:', token);

  if (!token) {
    return res.status(403).json({ message: 'Token requerido' });
  }

  try {
    const decoded = jwt.verify(token, SECRET_KEY);
    req.user = decoded;
    next();
  } catch (error) {
    res.status(401).json({ message: 'Token inválido' });
  }
};


// Ruta para subir archivos protegida por el middleware de verificación de token
app.post('/upload', verifyToken, upload.single('file'), (req, res) => {
  if (!req.file) {
    console.log('No se ha subido ningún archivo');
    return res.status(400).json({ message: 'No se ha subido ningún archivo.' });
  }

  // Enviar la respuesta con el nombre y la ruta del archivo subido
  const filePath = path.resolve('uploads', req.file.filename);
  console.log('Archivo subido:', filePath);
  res.json({
      nombre: req.file.originalname,
      ruta: filePath,
  });
});

// Ruta para obtener un archivo por nombre protegida por el middleware de verificación de token
app.get('/upload/:nombre', verifyToken, (req, res) => {
  console.log('Descargando archivo:', req.params.nombre);
  const nombreArchivo = req.params.nombre;
  const filePath = path.join(__dirname, 'uploads', nombreArchivo);

  // Verificar si el archivo existe
  if (fs.existsSync(filePath)) {
    res.download(filePath, (err) => {
      if (err) {
        res.status(500).send('Error al descargar el archivo');
      }
    });
  } else {
    res.status(404).send('Archivo no encontrado');
  }
});

// Iniciar el servidor
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Servidor corriendo en http://localhost:${PORT}`);
});
