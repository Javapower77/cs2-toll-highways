import { ModRegistrar } from "cs2/modding";

import { VanillaComponentResolver } from "./mods/VanillaComponentResolver";

const register: ModRegistrar = (moduleRegistry) => {

    VanillaComponentResolver.setRegistry(moduleRegistry);
//    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', TollInsightsPanel);
}

export default register;