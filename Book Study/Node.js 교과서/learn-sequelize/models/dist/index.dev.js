'use strict';

var Sequelize = require('sequelize');

var env = process.env.NODE_ENV || 'development';

var config = require('../config/config')[env];

var db = {};
var sequelize = new Sequelize(config.database, config.username, config.password, config);
db.sequelize = sequelize;
module.exports = db;