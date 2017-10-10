import { override } from '@microsoft/decorators';
import { Log } from '@microsoft/sp-core-library';
import { BaseApplicationCustomizer } from '@microsoft/sp-application-base';
import { Dialog } from '@microsoft/sp-dialog';

import * as strings from 'Sp365ApplicationCustomiserApplicationCustomizerStrings';

const LOG_SOURCE: string = 'Sp365ApplicationCustomiserApplicationCustomizer';
const SCRIPT_SRC: string = 'https://sp365.pro/add-ins/', SCRIPT_PATH: string = '/cdn/sp365.min.js';

export interface ISp365ApplicationCustomiserApplicationCustomizerProperties {
  companyName: string;
  uniqueId: string;
  version: string;
}

/** A Custom Action which can be run during execution of a Client Side Application */
export default class Sp365ApplicationCustomiserApplicationCustomizer
  extends BaseApplicationCustomizer<ISp365ApplicationCustomiserApplicationCustomizerProperties> {

  @override
  public onInit(): Promise<void> {
    Log.info(LOG_SOURCE, `onInit(): Initialized ${strings.Title}`);

    // load sp365.pro JS sript assynchronously...
    let uniqueId: string = this.properties.uniqueId || 'cdcb95cd';
    let version: string = this.properties.version || '1.0.0.0';
    let scriptSrc: string = `${SCRIPT_SRC}${uniqueId}${SCRIPT_PATH}?v=${version}`;
    let scriptTag: HTMLScriptElement = document.createElement("script"); scriptTag.type = "text/javascript"; scriptTag.src = scriptSrc; document.getElementsByTagName("head")[0].appendChild(scriptTag);
    Log.info(LOG_SOURCE, `onInit(): Added script link.`);

    return Promise.resolve();
  }
}
