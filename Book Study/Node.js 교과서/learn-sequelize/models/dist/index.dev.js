'use strict';

var Sequelize = require('sequelize');

var User = require('./user');

var Comment = require('./comment');

var env = process.env.NODE_ENV || 'development';

var config = require('../config/config')[env];

var db = {};
var sequelize = new Sequelize(config.database, config.username, config.password, config);
db.sequelize = sequelize;
db.User = User;
db.Comment = Comment;
User.init(sequelize);
Comment.init(sequelize);
User.associate(db);
Comment.associate(db);
module.exports = db;