'use strict';

const gulp = require('gulp');
const build = require('@microsoft/sp-build-web');

build.initialize(gulp);

// overrides:
// .\node_modules\@microsoft\sp-build-core-tasks\lib\packageSolution\packageSolution\xmlGenerators\createFeatureXml.js
//      function createFeatureXml(feature, resources) {
//          ...
//          Scope: feature.scope||'Web',
//          Hidden: (feature.hidden?'TRUE':'FALSE')
//          ...
//      }
// .\node_modules\@microsoft\sp-build-core-tasks\lib\packageSolution\packageSolution\xmlGenerators\createSolutionXml.js
//      function createSolutionXml(solution, customFeatureFilenames, resources) {
//          ...
//          if (!solution.startPage) { properties.push({ StartPage: solution.startPage }); }
//          if (!solution.settingsPage) { properties.push({ SettingsPage: solution.settingsPage }); }
//          if (!solution.installedEventEndpoint) { properties.push({ InstalledEventEndpoint: solution.installedEventEndpoint }); }
//          if (!solution.uninstallingEventEndpoint) { properties.push({ UninstallingEventEndpoint: solution.uninstallingEventEndpoint }); }
//          if (!solution.upgradedEventEndpoint) { properties.push({ UpgradedEventEndpoint: solution.upgradedEventEndpoint }); }
//          ...
//      }


// NOTE: task only to execute the sub-task called 'package-solution'
//gulp.task('package', ['package-solution']);

// ... IsClientSideSolution="false"
// <StartPage>https://sp365.pro/add-ins/cdcb95cd?{StandardTokens}</StartPage><SettingsPage>https://sp365.pro/add-ins/cdcb95cd/settings?{StandardTokens}</SettingsPage><InstalledEventEndpoint>https://sp365-aps.azurewebsites.net/Services/AppEventReceiver.svc</InstalledEventEndpoint><UninstallingEventEndpoint>https://sp365-aps.azurewebsites.net/Services/AppEventReceiver.svc</UninstallingEventEndpoint><UpgradedEventEndpoint>https://sp365-aps.azurewebsites.net/Services/AppEventReceiver.svc</UpgradedEventEndpoint>
// <AppPrincipal><RemoteWebApplication ClientId="*" /></AppPrincipal><AppPermissionRequests AllowAppOnlyPolicy="true" ><AppPermissionRequest Scope="http://sharepoint/content/sitecollection" Right="FullControl" /></AppPermissionRequests><RemoteEndpoints><RemoteEndpoint Url="https://sp365.pro" /><RemoteEndpoint Url="https://sp365-aps.azurewebsites.net" /><RemoteEndpoint Url="https://sp365.sopeapps.com.au" /></RemoteEndpoints>
