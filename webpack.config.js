/* tslint:disable object-literal-sort-keys */
var webpack = require('webpack');
// const CircularDependencyPlugin = require('circular-dependency-plugin');

var commonFiles = ['es6-shim', 'dom4', './src/_polyfills'];

var config = {};
config.entry = {
    contentEditor: [...commonFiles, './src/entry/contentEditor']
};

config.output = {
    path: '/build',
    filename: '[name].bundle.js'
};

config.resolve = {
    extensions: ['.ts', '.tsx', '.js']
};

config.module = {
    rules: [
        {
            test: /worker\.ts$/,
            use: {
                loader: 'worker-loader',
                options: {
                    fallback: false,
                    inline: true,
                    name: '[name].worker.js'
                }
            }
        },
        {
            test: /\.tsx?$/,
            use: 'ts-loader'
        },
        {
            test: /\.json$/,
            use: 'json-loader'
        },
        {
            test: /\.s?css$/,
            use: ['style-loader', 'css-loader', 'sass-loader']
        },
        {
            test: /(icons|section_layout_builder_rows)[\\\/][^\\\/]+\.svg$/i,
            use: 'svg-inline-loader'
        },
        {
            test: /\.(png|gif|jpe?g|eot|woff|woff2|ttf)(\?.*)?$/i,
            use: {
                loader: 'file-loader',
                options: {
                    name: '/publisher/Styles/compiled/[hash].[ext]'
                }
            }
        }
    ]
};

config.plugins = [
    new webpack.optimize.CommonsChunkPlugin({
        name: 'commons',
        filename: 'commons.bundle.js',
        minChunks: Object.keys(config.entry).length // lab and empty entrypoints are not counted
    }),

    // ignore locales from the momentjs package
    new webpack.IgnorePlugin(/^\.\/locale$/, /moment$/)

    // debug curcular dependencies in the imports
    // new CircularDependencyPlugin({failOnError: false, cwd: process.cwd()})
];

try {
    var userConfig = require('./webpack.my.config.js');
    if (userConfig) {
        config = userConfig(config);
    }
}
catch (err) {
    // no custom webpack configuration
}

module.exports = config;
