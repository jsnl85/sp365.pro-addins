// npm install gulp lodash fs gulp-less gulp-header gulp-clean-css gulp-rename webpack-stream gulp-uglify gulp-javascript-obfuscator browser-sync owl.carousel vinyl-ftp

// Task Scripts
var gulp = require('gulp');
var gutil = require('gulp-util');
const _ = require('lodash');
const fs = require('fs');
var less = require('gulp-less');
var browserSync = require('browser-sync').create();
var header = require('gulp-header');
var cleanCSS = require('gulp-clean-css');
var rename = require("gulp-rename");
var webpack = require('webpack-stream');
var uglify = require('gulp-uglify');
const obfuscator = require('gulp-javascript-obfuscator');
const vinylFtp = require('vinyl-ftp');
// Styles
//const sass = require('gulp-sass');
//const bless = require('gulp-bless');
//const imgmin = require('gulp-imagemin');
// linting (TODO)
//const parker = require('gulp-parker');
//const sasslint = require('gulp-sass-lint');
// Configuration
var pkg = require('./package.json');

// Set the banner content
var banner = ['/*!\n',
    ' * Start Bootstrap - <%= pkg.title %> v<%= pkg.version %> (<%= pkg.homepage %>)\n',
    ' * Copyright 2013-' + (new Date()).getFullYear(), ' <%= pkg.author %>\n',
    ' * Licensed under <%= pkg.license.type %> (<%= pkg.license.url %>)\n',
    ' */\n',
    ''
].join('');

// Create the Configuration.json file
gulp.task('configure', function() {
    var cfgDefaults = require('./configuration.defaults.json');
    // 
    // ensure configuration.json file exists (i.e. it is in .gitignore)
    if(!fs.existsSync('configuration.json')) { const cfgDefaultsContent = fs.readFileSync('./configuration.defaults.json'); fs.writeFileSync('configuration.json', cfgDefaultsContent); }
    cfg = require('./configuration.json');
    // 
    // apply new properties to Configuration.json file
    cfg = _.defaultsDeep(cfg, cfgDefaults);
    try {
        const cfgContent = JSON.stringify(cfg, null, 2);
        const cfgContentCurrent = fs.readFileSync('./configuration.json');
        if (cfgContent != cfgContentCurrent) {
            fs.writeFileSync('./configuration.json', cfgContent, 'utf-8');
            console.log("Updated configuration.json file");
        }
    }
    catch (err) { console.log('Error writing Configuration.json:' + err.message); }
});

// Compile LESS files from /less into /css
gulp.task('less', function() {
    return gulp.src('less/creative.less')
        .pipe(less().on('error', function(err){
            gutil.log(err);
            this.emit('end');
        }))
        .pipe(header(banner, { pkg: pkg }))
        .pipe(gulp.dest('css'))
        .pipe(browserSync.reload({
            stream: true
        }))
});

// Minify compiled CSS
gulp.task('minify-css', ['less'], function() {
    return gulp.src('css/creative.css')
        .pipe(cleanCSS({ compatibility: 'ie8' }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest('css'))
        .pipe(browserSync.reload({
            stream: true
        }))
});

// Minify JS
gulp.task('minify-js', function() {
    return gulp.src('js/creative.js')
        .pipe(uglify())
        .pipe(header(banner, { pkg: pkg }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest('js'))
        .pipe(browserSync.reload({
            stream: true
        }))
});

// Copy vendor libraries from /node_modules into /vendor
gulp.task('copy', function() {
    gulp.src(['node_modules/bootstrap/dist/**/*', '!**/npm.js', '!**/bootstrap-theme.*', '!**/*.map'])
        .pipe(gulp.dest('vendor/bootstrap'))

    gulp.src(['node_modules/jquery/dist/jquery.js', 'node_modules/jquery/dist/jquery.min.js'])
        .pipe(gulp.dest('vendor/jquery'))

    gulp.src(['node_modules/magnific-popup/dist/*'])
        .pipe(gulp.dest('vendor/magnific-popup'))

    gulp.src(['node_modules/scrollreveal/dist/*.js'])
        .pipe(gulp.dest('vendor/scrollreveal'))

    gulp.src([
            'node_modules/font-awesome/**',
            '!node_modules/font-awesome/**/*.map',
            '!node_modules/font-awesome/.npmignore',
            '!node_modules/font-awesome/*.txt',
            '!node_modules/font-awesome/*.md',
            '!node_modules/font-awesome/*.json'
        ])
        .pipe(gulp.dest('vendor/font-awesome'))

    //gulp.src(['node_modules/owl.carousel/dist/*.js', 'node_modules/owl.carousel/dist/assets/*'])
    //    .pipe(gulp.dest('vendor/owl.carousel'))
})

// FTP Configuration
// helper function to build an FTP connection based on our configuration
function getFtpConnection(cfgFtp) {
    return vinylFtp.create({
        host: cfgFtp.host,
        port: cfgFtp.port,
        user: cfgFtp.username, //process.env.FTP_USER
        password: cfgFtp.password, //process.env.FTP_PWD
        parallel: 5,
        log: gutil.log
    });
}
function getFtpFilesToUpload(cfgFtp) {
    //var localFilesGlob = [ "./**/*" ];
    var localFilesGlob = [
        '*',
        '.htaccess',
        '!app_offline.htm',
        'add-ins/*','add-ins/.htaccess',
        'add-ins/398099f4/*', 'add-ins/398099f4/cdn/.htaccess', 'add-ins/398099f4/cdn/**', 'add-ins/398099f4/pkg/**', '!add-ins/398099f4/src',
        'add-ins/cdcb95cd/*', 'add-ins/cdcb95cd/cdn/.htaccess', 'add-ins/cdcb95cd/cdn/**', 'add-ins/cdcb95cd/pkg/**', '!add-ins/cdcb95cd/src',
        '!src',
        'bundle/**', //'!bundle',
        //'contacts/**',
        'css/**',
        '!design',
        'favicon/**',
        'img/**',
        'js/**',
        '!less',
        '!node_modules',
        'projects/**',
        'sections/**',
        'seo/**',
        'services/**',
        'about-us/**',
        'vendor/**',
        '!.gitignore',
        '!config*.json',
        '!package*.json',
        '!gulpfile.js',
        '!license',
        '!readme*',
        '!settings.js'
        //'!web.config'
    ];
    return localFilesGlob;
}

/**
 * Deploy task.
 * Copies the new files to the server
 *
 * Usage: `FTP_USER=someuser FTP_PWD=somepwd gulp ftp-deploy`
 */
gulp.task('ftp-deploy', ['configure'], function() {
    var cfg = require('./configuration.json');
    var cfgFtp = cfg.ftp; //(cfg||{}).ftp||((cfg||{}).ftp||[])[0]||{};
    // 
    var conn = getFtpConnection(cfgFtp);
    var localFilesGlob = getFtpFilesToUpload(cfgFtp);
    // 
    return gulp.src(localFilesGlob, { base: '.', buffer: false })
        .pipe( conn.newer( cfgFtp.remoteFolder ) ) // only upload newer files 
        .pipe( conn.dest( cfgFtp.remoteFolder ) )
    ;
});

/**
 * Watch deploy task.
 * Watches the local copy for changes and copies the new files to the server whenever an update is detected
 *
 * Usage: `FTP_USER=someuser FTP_PWD=somepwd gulp ftp-deploy-watch`
 */
gulp.task('ftp-deploy-watch', ['configure'], function() {
    var cfg = require('./configuration.json');
    var cfgFtp = cfg.ftp; //(cfg||{}).ftp||((cfg||{}).ftp||[])[0]||{};
    // 
    var conn = getFtpConnection(cfgFtp);
    var localFilesGlob = getFtpFilesToUpload(cfgFtp);
    // 
    gulp.watch(localFilesGlob)
      .on('change', function(event) {
        console.log('Changes detected! Uploading file "' + event.path + '", ' + event.type);

        return gulp.src( [event.path], { base: '.', buffer: false } )
          .pipe( conn.newer( cfgFtp.remoteFolder ) ) // only upload newer files 
          .pipe( conn.dest( cfgFtp.remoteFolder ) )
        ;
      })
    ;
});


// Run everything
gulp.task('default', ['configure', 'less', 'minify-css', 'minify-js', 'copy']);

// Configure the browserSync task
gulp.task('browserSync', function() {
    browserSync.init({
        server: {
            baseDir: ''
        },
    })
})

// Dev task with browserSync
gulp.task('dev', ['browserSync', 'less', 'minify-css', 'minify-js'], function() {
    gulp.watch('less/*.less', ['less']);
    gulp.watch('css/*.css', ['minify-css']);
    gulp.watch('js/*.js', ['minify-js']);
    // Reloads the browser whenever HTML or JS files change
    gulp.watch('*.html', browserSync.reload);
    gulp.watch('js/**/*.js', browserSync.reload);
});
