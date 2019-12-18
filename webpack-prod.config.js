var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');
var BomPlugin = require('webpack-utf8-bom')

var config = require('./webpack.config.js');
var extractCss = new ExtractTextPlugin({
    allChunks: true,
    filename: 'bundle.css'
})

delete config.entry.lab;

config.output.path = __dirname + '/build';
config.plugins = config.plugins.concat([

    extractCss,

    new webpack.DefinePlugin({
        'process.env': {
            'NODE_ENV': JSON.stringify('production'),
			'apiUrl': JSON.stringify('../api')
        }
    }),

    new webpack.LoaderOptionsPlugin({
        debug: false,
        minimize: true
    }),

    // add UTF8 BOM to the output file
    // this is necessary due to some issues on CK editor, see https://github.com/ckeditor/ckeditor5-design/issues/36
    new BomPlugin(true)
]);

for (var i = 0; i < config.module.rules.length; i++) {
    var rules = !!config.module.rules[i].use.indexOf ? config.module.rules[i].use : [];
    if (typeof rules === 'object' && rules.length > 0) {
        config.module.rules[i].use = extractCss.extract(['css-loader', 'sass-loader']);
    }
}

module.exports = config;
